import datetime
import os

import cv2
from flask import Flask, jsonify, render_template, request, send_from_directory, url_for
from werkzeug.utils import secure_filename

from src.image_processing import edge_detection, init_net
from src.utils import format_bytes

app = Flask(__name__, static_folder='src/images')

# Configure upload folders
BASE_DIR = os.path.abspath(os.path.dirname(__file__))
RAW_FOLDER = os.path.join(BASE_DIR, 'images/raw')
PROCESSED_FOLDER = os.path.join(BASE_DIR, 'images/processed')
ALLOWED_EXTENSIONS = {'png', 'jpg', 'jpeg', 'gif'}
ML2_FOLDER = os.path.join(BASE_DIR, 'images/ml2_exchange')

# Ensure directories exist
os.makedirs(RAW_FOLDER, exist_ok=True)
os.makedirs(PROCESSED_FOLDER, exist_ok=True)
os.makedirs(ML2_FOLDER, exist_ok=True)  
os.makedirs(os.path.join(PROCESSED_FOLDER, 'data'), exist_ok=True) # for exchanging other formats
os.makedirs(os.path.join(RAW_FOLDER, 'data'), exist_ok=True)


def allowed_file(filename):
    return '.' in filename and filename.rsplit('.', 1)[1].lower() in ALLOWED_EXTENSIONS


def get_images_info(folder, path_type):
    images = []
    for filename in os.listdir(folder):
        filepath = os.path.join(folder, filename)
        if os.path.isfile(filepath) and allowed_file(filename):
            size = os.path.getsize(filepath)
            date = datetime.datetime.fromtimestamp(os.path.getctime(filepath)).strftime('%Y-%m-%d %H:%M:%S')
            images.append({
                'filename': filename,
                'path': url_for('fetch_image', path_type=path_type, filename=filename),#f"/{folder}/{filename}",
                'size': format_bytes(size),
                'date': date
            })
    return images


def process_image_from_file(file_path, net, save_path):
    image = cv2.imread(file_path)
    edge_detection_output = edge_detection(file_path, image, net, save_path)
    return edge_detection_output


@app.route("/")
def home():
    return render_template("index.html")

"""
A gallery to preview the images
"""
@app.route("/images/<path_type>/gallery")
def gallery(path_type):
    print("type: ", path_type)
    if path_type == "processed":
        folder = PROCESSED_FOLDER
    elif path_type == "raw":
        folder = RAW_FOLDER
    else:
        return f"path type \"{path_type}\" not found", 404
    
    images = get_images_info(folder, path_type)
    return render_template("gallery.html", path_type=path_type, images=images)


"""
Upload raw or processed images to their respective folder on the server
"""
@app.route("/images/<path_type>", methods=['GET', 'POST'])
def upload_image(path_type):
    try:
        if path_type not in ['processed', 'raw']:
            return "Invalid path type", 404
        folder = PROCESSED_FOLDER if path_type == "processed" else RAW_FOLDER
        if request.method == 'POST':
            if 'file' not in request.files:
                return "No file part", 400
            file = request.files['file']
            if file.content_length == 0:
                return jsonify({"error": "Uploading an empty file is not allowed"}), 400
            if file and allowed_file(file.filename):
                filename = secure_filename(file.filename)
                file.save(os.path.join(folder, filename))
                print(f"file saved to {os.path.join(folder, filename)}")
                return jsonify({"status": "success", "path_type": path_type}), 200
        return render_template("upload.html", path_type=path_type)
    except Exception as e:
        print(f"Error in upload_image: {e}")
        return "Internal Server Error", 500


"""
Access image of a specific type
"""
@app.route("/images/<path_type>/<filename>")
def fetch_image(path_type, filename):
    folder = PROCESSED_FOLDER if path_type == "processed" else RAW_FOLDER
    return send_from_directory(folder, filename)


"""
The Data folder is used to keep processed / raw data of different types. This will be useful if we 
Use other representations for the ref image projected on the headset. E.g. image graphs
"""
@app.route("/images/<path_type>/data")
def data_folder(path_type):
    if path_type not in ['processed', 'raw']:
        return "Invalid path type", 404
    # folder = os.path.join(PROCESSED_FOLDER, 'data') if path_type == "processed" else os.path.join(RAW_FOLDER, 'data')
    # The data folder functionality is currently empty.
    return "Data folder is currently empty.", 200


"""
Not sure if this is still used 
"""
@app.route('/images/<path:path>')
def serve_image(path):
    image_path = os.path.join('images', path)
    if not os.path.exists(image_path):
        return "Image not found", 404
    return send_from_directory(BASE_DIR, image_path)


"""
    Actions initiated by the user

    Handles requests from ML2 for sending and receiving images.
    ML2 sends an image to the server, which processes it and returns the result
    
    ML2 -- GET
"""
@app.route("/comm_usr", methods=["GET"])
def comm_usr():
    # Send the most recent processed image to ML2
    if request.method == "GET":
        files = [f for f in os.listdir(PROCESSED_FOLDER) if allowed_file(f)]
        if not files:
            return "No processed images available", 404
        # latest_file = max(files, key=lambda x: os.path.getctime(os.path.join(PROCESSED_FOLDER, x)))
        key = request.args.get("img")
        print("requested image: ", key)
        for f in files:
            if f == key:
                print("found image at: ", f)
                return send_from_directory(PROCESSED_FOLDER, f)
        return f"No processed image found with name \"{key}_transparent.png\"", 500

    return "Invalid method", 405


"""
    Actions initiated by the client (ML2). 

    Laptop continuously polls this endpoint to fetch images for processing.
    If an image exists in the ML2 folder, it processes and stores the result.
    
    ML2 -- POST
    User -- GET
"""
@app.route("/comm_ml2", methods=["GET", "POST"])
def comm_ml2():
    if request.method == 'GET':
        # the server fetches the posted image by ML2, processes it, then saves it in the 'processed' folder
        files = [f for f in os.listdir(ML2_FOLDER) if allowed_file(f)]
        if not files:
            return "No new images from ML2", 404
        for filename in files:
            filepath = os.path.join(ML2_FOLDER, filename)
            try:
                # Process the image
                processed_path = os.path.join(PROCESSED_FOLDER, filename)
                process_image_from_file(filepath, net, save_path=processed_path)
                print(f"Processed image saved to {processed_path}")

                # Delete raw image after processing
                # os.remove(filepath)

                # Return success message
                return jsonify({
                    "status": "success",
                    "filename": filename,
                    "processed_path": processed_path
                })
            except Exception as e:
                return jsonify({"status": "error", "Error while fetching images posted by ML2": str(e)}), 500
        return "No images processed", 200
    
    # the ML2 device posts a new images to the server for processing
    elif request.method == 'POST':
        if 'file' not in request.files:
            return "No file part", 400
        file = request.files['file']
        if file and allowed_file(file.filename):
            try:
                filename = secure_filename(file.filename)
                filepath = os.path.join(ML2_FOLDER, filename)
                file.save(filepath)
                print(f"Image received from ML2 and saved to {filepath}")
            except Exception as e:
                return jsonify({"status": "error", "Error while saving the image posted by ML2": str(e)}), 500
                


if __name__ == "__main__":
    # Initialize image processor
    net = init_net()
    
    # run the server
    app.run(host='0.0.0.0', port=80)