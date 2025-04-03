import cv2
import numpy as np
import matplotlib
matplotlib.use('agg')
import matplotlib.pyplot as plt
from matplotlib.widgets import Slider
from tqdm import tqdm

import os

class CropLayer(object):
    def __init__(self, params, blobs):
        # initialize our starting and ending (x, y)-coordinates of
        # the crop
        self.startX = 0
        self.startY = 0
        self.endX = 0
        self.endY = 0

    def getMemoryShapes(self, inputs):
        # the crop layer will receive two inputs -- we need to crop
        # the first input blob to match the shape of the second one,
        # keeping the batch size and number of channels
        (inputShape, targetShape) = (inputs[0], inputs[1])
        (batchSize, numChannels) = (inputShape[0], inputShape[1])
        (H, W) = (targetShape[2], targetShape[3])
        # compute the starting and ending crop coordinates
        self.startX = int((inputShape[3] - targetShape[3]) / 2)
        self.startY = int((inputShape[2] - targetShape[2]) / 2)
        self.endX = self.startX + W
        self.endY = self.startY + H
        # return the shape of the volume (we'll perform the actual
        # crop during the forward pass
        return [[batchSize, numChannels, H, W]]

    def forward(self, inputs):
        # use the derived (x, y)-coordinates to perform the crop
        return [inputs[0][:, :, self.startY:self.endY,
                self.startX:self.endX]]

def init_net():
    # load our serialized edge detector from disk
    protoPath = "src/model/deploy.prototxt"
    modelPath = "src/model/hed_pretrained_bsds.caffemodel"
    cv2.dnn_registerLayer("Crop", CropLayer)
    net = cv2.dnn.readNetFromCaffe(protoPath, modelPath)
    return net

def preprocess_image(image):
    # Enhance contrast with CLAHE
    clahe = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8, 8))
    lab_image = cv2.cvtColor(image, cv2.COLOR_BGR2LAB)
    lab_image[:, :, 0] = clahe.apply(lab_image[:, :, 0])  # Apply CLAHE only on L channel (luminance)
    enhanced_image = cv2.cvtColor(lab_image, cv2.COLOR_LAB2BGR)

    # Sharpen the image
    sharpen_kernel = np.array([[0, -1, 0],
                            [-1, 5, -1],
                            [0, -1, 0]])
    sharpened_image = cv2.filter2D(enhanced_image, -1, sharpen_kernel)

    # Optionally apply Gaussian Blur to reduce noise
    # preprocessed_image = cv2.GaussianBlur(sharpened_image, (5, 5), 0)

    return sharpened_image


def edge_detection(image_path, image, net, save_path=None):
    (H, W) = image.shape[:2]
    blob = cv2.dnn.blobFromImage(image, scalefactor=1.0, size=(W, H),
        mean=(104.00698793, 116.66876762, 122.67891434),
        swapRB=False, crop=False)

    net.setInput(blob)
    hed = net.forward()
    hed = cv2.resize(hed[0, 0], (W, H))
    hed = (255 * hed).astype("uint8")

    # Create a mask for the inner region and set 10-pixels of border to 0
    mask = np.ones_like(hed, dtype=np.uint8)
    border_thickness = 10
    mask[:border_thickness, :] = 0  # Top border
    mask[-border_thickness:, :] = 0  # Bottom border
    mask[:, :border_thickness] = 0  # Left border
    mask[:, -border_thickness:] = 0  # Right border
    cleaned_hed_output = cv2.bitwise_and(hed, hed, mask=mask)

    # Turn every non-black pixel into pure white (strictly black and white image)
    binary_image = np.where(cleaned_hed_output > 64, 255, 0).astype("uint8")
    
    # Invert the colors (black becomes white, white becomes black)
    inverted_image = 255 - binary_image

    # Convert to RGBA and make the white part transparent
    rgba_image = cv2.cvtColor(inverted_image, cv2.COLOR_GRAY2BGRA)
    rgba_image[:, :, 3] = np.where(inverted_image == 255, 0, 255)

    # Save the resulting RGBA image
    # TODO: separate save folder
    if save_path is not None:
        file_name = image_path.split('/')[-1]
        cv2.imwrite(os.path.join(save_path, file_name[:-4] + '_transparent.png'), rgba_image)
        cv2.imwrite(os.path.join(save_path, file_name[:-4] + '_hed.png'), hed)
    return rgba_image
            

# based on https://medium.com/swlh/contours-in-images-a58b4c12c0ff
def contour_moments(image, save_path=None):
    img = cv2.imread(image, 1)
    imgray = cv2.cvtColor(img,cv2.COLOR_BGR2GRAY)
    thresh = cv2.adaptiveThreshold(imgray,255,cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY,11,2)

    # Write function to find contours
    # - Retrieval - TREE
    # - Approximation - Simple
    contours, hierarchy = cv2.findContours(thresh,cv2.RETR_TREE,cv2.CHAIN_APPROX_SIMPLE)

    # drawing contours over blank image
    ctr = np.zeros(img.shape, dtype=np.uint8)
    cv2.drawContours(ctr, contours, -1, (0,255,0), 3)

    # drawing contours over original image
    img_with_contours = img.copy()
    cv2.drawContours(img_with_contours, contours, -1, (0,255,0), 3) # Talk about (0, 255, 0) colors
    print("Number of contours = {}".format(len(contours)))
    f = plt.figure(figsize=(15,15))
    f.add_subplot(2, 2, 1).set_title('Original Image')
    plt.imshow(img[:,:,::-1])
    f.add_subplot(2, 2, 2).set_title('Thresholded Image')
    plt.imshow(thresh, cmap="gray")
    f.add_subplot(2, 2, 3).set_title('Contours standalone')
    plt.imshow(ctr[:,:,::-1])
    f.add_subplot(2, 2, 4).set_title('Original Imagewith contours')
    plt.imshow(img_with_contours[:,:,::-1])
    
    if save_path is not None:
        plt.savefig(f"{save_path}/moments.png")

    plt.show()

def contour_length_based(image, save_path=None):
    img = cv2.imread(image, 0)
    thresh=cv2.adaptiveThreshold(img,255,cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY,11,2)
    contours, hierarchy= cv2.findContours(thresh,cv2.RETR_TREE,cv2.CHAIN_APPROX_SIMPLE);

    # # drawing contours over original image
    # img_with_contours = img.copy()
    # img_with_contours = cv2.cvtColor(img, cv2.COLOR_GRAY2BGR)
    # cv2.drawContours(img_with_contours, contours, 6, (0,255,0), 3); # Talk about (0, 255, 0) colors

    # plt.figure(figsize=(8, 8))
    # plt.imshow(img_with_contours[:,:,::-1])

    # contour
    cnt = contours[6]
    epsilon = 0.1*cv2.arcLength(cnt,True)
    approx = cv2.approxPolyDP(cnt,epsilon,True)

    # drawing contours over original image
    img_with_contours = img.copy()
    img_with_contours = cv2.cvtColor(img, cv2.COLOR_GRAY2BGR)
    cv2.drawContours(img_with_contours, [approx], 0, (0,255,0), 3) # Talk about (0, 255, 0) colors


    plt.figure(figsize=(8, 8))
    plt.imshow(img_with_contours[:,:,::-1])

    if save_path is not None:
        plt.savefig(f"{save_path}/length_based.png")
    
    plt.show()

def contour_convex_hull(image, save_path=None):
    img = cv2.imread(image, 0)  
    thresh = cv2.adaptiveThreshold(img,255,cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY,11,2) 
    contours, hierarchy = cv2.findContours(thresh,cv2.RETR_TREE,cv2.CHAIN_APPROX_SIMPLE)
    hull = []
    
    # calculate points for each contour
    for i in range(len(contours)):
        # creating convex hull object for each contour
        hull.append(cv2.convexHull(contours[i], False))
    drawing = np.zeros((thresh.shape[0], thresh.shape[1], 3), np.uint8)
    
    # draw contours and hull points
    for i in tqdm(range(len(contours)), desc='computing convex hulls'):
        color_contours = (0, 255, 0) # green - color for contours
        color = (255, 0, 0) # blue - color for convex hull
        # draw ith contour
        cv2.drawContours(drawing, contours, i, color_contours, 1, 8, hierarchy)
        # draw ith convex hull object
        cv2.drawContours(drawing, hull, i, color, 1, 8)
    plt.figure(figsize=(8, 8))
    plt.imshow(drawing[:,:,::-1])

    if save_path is not None:
        plt.savefig(f"{save_path}/convex_hull.png")
    plt.show()


def bounding_contour(image, save_path=None):
    img = cv2.imread(image, 0)

    # Change thresholding and check 
    #ret,thresh = cv2.threshold(imgray,127,255,0) 
    ret,thresh = cv2.threshold(img,127,255,0) 
    #ret,thresh = cv2.threshold(imgray,0,255,cv2.THRESH_BINARY+cv2.THRESH_OTSU)

    contours, hierarchy = cv2.findContours(thresh,cv2.RETR_TREE,cv2.CHAIN_APPROX_SIMPLE)  

    # drawing contours over original image 
    img_with_contours = img.copy() 
    img_with_contours = cv2.cvtColor(img, cv2.COLOR_GRAY2BGR)
    cv2.drawContours(img_with_contours, contours, 1, (0,255,0), 3) 

    # Talk about (0, 255, 0) colors   
    plt.figure(figsize=(8, 8)) 
    plt.imshow(img_with_contours[:,:,::-1]) 
    
    if save_path is not None:
        plt.savefig(f"{save_path}/bounding_contour.png")
    plt.show()


def sobel_filter(image, save_path=None):
    # Read the image
    img = cv2.imread(image)

    # Convert the image to grayscale
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

    # Apply Sobel filter in the X direction
    sobelx = cv2.Sobel(src=gray, ddepth=cv2.CV_64F, dx=3, dy=0, ksize=5)

    # Apply Sobel filter in the Y direction
    sobely = cv2.Sobel(src=gray, ddepth=cv2.CV_64F, dx=0, dy=3, ksize=5)

    # Compute the gradient magnitude
    sobel_combined = cv2.magnitude(sobelx, sobely)

    # Normalize the gradient images for display
    sobelx = cv2.convertScaleAbs(sobelx)
    sobely = cv2.convertScaleAbs(sobely)
    sobel_combined = cv2.convertScaleAbs(sobel_combined)

    # Convert BGR to RGB for displaying with matplotlib
    img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)

    # Plot the results
    plt.figure(figsize=(12, 8))

    plt.subplot(2, 2, 1)
    plt.imshow(img_rgb)
    plt.title('Original Image')
    plt.axis('off')

    plt.subplot(2, 2, 2)
    plt.imshow(gray, cmap='gray')
    plt.title('Grayscale Image')
    plt.axis('off')

    plt.subplot(2, 2, 3)
    plt.imshow(sobelx, cmap='gray')
    plt.title('Sobel X')
    plt.axis('off')

    plt.subplot(2, 2, 4)
    plt.imshow(sobely, cmap='gray')
    plt.title('Sobel Y')
    plt.axis('off')

    plt.figure(figsize=(6, 6))
    plt.imshow(sobel_combined, cmap='gray')
    plt.title('Sobel Gradient Magnitude')
    plt.axis('off')

    if save_path is not None:
        plt.savefig(f"{save_path}_sobel.png") # ensure that save_path is of the form "/path/to/dir/some_name"
    # plt.show()


def sobel_interactive(image, save_path=None):

    # Read the image
    img = cv2.imread(image)

    # Convert BGR to RGB for displaying with matplotlib
    img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)

    # Convert the image to grayscale
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

    # Initial parameters
    init_dx = 1
    init_dy = 1
    init_ksize_x = 3  # Must be odd and in the range [1, 31]
    init_ksize_y = 3  # Must be odd and in the range [1, 31]
    derivative_orderx = max(1, min(int(init_dx + init_dy), init_ksize_x))
    derivative_ordery = max(1, min(int(init_dx + init_dy), init_ksize_y))

    # Apply Sobel filters with initial parameters
    def apply_sobel(dx, dy, ksize, dir):
        # Ensure ksize is odd and within valid range
        ksize = int(ksize)
        if ksize % 2 == 0:
            ksize += 1
        ksize = max(1, min(ksize, 31))
        if ksize > 1 and ksize % 2 == 0:
            ksize += 1

        # Ensure derivative order does not exceed kernel size
        if dir == 'x':
            derivative_orderx = max(1, min(int(dx + dy), ksize))
        elif dir == 'y':
            derivative_ordery = max(1, min(int(dx + dy), ksize))
        else:
            raise RuntimeError("direction must be one of 'x' and 'y' ")

        sobel = cv2.Sobel(src=gray, ddepth=cv2.CV_64F, dx=dx, dy=dy, ksize=ksize)
        sobel = cv2.convertScaleAbs(sobel)
        return sobel
    
    sobelx = apply_sobel(init_dx, 0, init_ksize_x, 'x')
    sobely = apply_sobel(0, init_dy, init_ksize_y, 'y')
    sobel_combined = cv2.magnitude(sobelx.astype(np.float32), sobely.astype(np.float32))
    sobel_combined = cv2.convertScaleAbs(sobel_combined)

    # Create the plot
    fig, axes = plt.subplots(2, 2, figsize=(12, 8))
    plt.subplots_adjust(left=0.25, bottom=0.35)

    # Display images
    ax_orig = axes[0, 0]
    ax_sobelx = axes[0, 1]
    ax_sobely = axes[1, 0]
    ax_combined = axes[1, 1]

    ax_orig.imshow(img_rgb)
    ax_orig.set_title('Original Image')
    ax_orig.axis('off')

    im_sobelx = ax_sobelx.imshow(sobelx, cmap='gray')
    ax_sobelx.set_title('Sobel X')
    ax_sobelx.axis('off')

    im_sobely = ax_sobely.imshow(sobely, cmap='gray')
    ax_sobely.set_title('Sobel Y')
    ax_sobely.axis('off')

    im_combined = ax_combined.imshow(sobel_combined, cmap='gray')
    ax_combined.set_title('Gradient Magnitude')
    ax_combined.axis('off')

    # Create sliders for dx, dy, ksize_x, and ksize_y
    ax_dx = plt.axes([0.25, 0.25, 0.65, 0.03])
    ax_dy = plt.axes([0.25, 0.20, 0.65, 0.03])
    ax_ksize_x = plt.axes([0.25, 0.15, 0.65, 0.03])
    ax_ksize_y = plt.axes([0.25, 0.10, 0.65, 0.03])

    slider_dx = Slider(ax_dx, 'dx', 1, derivative_orderx, valinit=init_dx, valstep=1)
    slider_dy = Slider(ax_dy, 'dy', 1, derivative_ordery, valinit=init_dy, valstep=1)
    slider_ksize_x = Slider(ax_ksize_x, 'ksize_x', 1, 31, valinit=init_ksize_x, valstep=2)
    slider_ksize_y = Slider(ax_ksize_y, 'ksize_y', 1, 31, valinit=init_ksize_y, valstep=2)

    # Update function
    def update(val):
        dx = int(slider_dx.val)
        dy = int(slider_dy.val)
        ksize_x = int(slider_ksize_x.val)
        ksize_y = int(slider_ksize_y.val)

        # Ensure ksize is odd and within valid range
        if ksize_x % 2 == 0:
            ksize_x += 1
        ksize_x = max(1, min(ksize_x, 31))

        if ksize_y % 2 == 0:
            ksize_y += 1
        ksize_y = max(1, min(ksize_y, 31))

        # Adjust dx and dy if they exceed ksize
        dx = min(dx, ksize_x)
        dy = min(dy, ksize_y)

        # Update sliders if dx or dy were adjusted
        slider_dx.set_val(dx)
        slider_dy.set_val(dy)

        # Update Sobel X
        sobelx = apply_sobel(dx, 0, ksize_x)
        im_sobelx.set_data(sobelx)

        # Update Sobel Y
        sobely = apply_sobel(0, dy, ksize_y)
        im_sobely.set_data(sobely)

        # Update Combined Gradient
        sobel_combined = cv2.magnitude(sobelx.astype(np.float32), sobely.astype(np.float32))
        sobel_combined = cv2.convertScaleAbs(sobel_combined)
        im_combined.set_data(sobel_combined)
        fig.canvas.draw_idle()

    # Connect the sliders to the update function
    slider_dx.on_changed(update)
    slider_dy.on_changed(update)
    slider_ksize_x.on_changed(update)
    slider_ksize_y.on_changed(update)

    plt.show()
    

if __name__ == '__main__':
    # Folder containing the images
    image_folder = "./images/raw"
    net = init_net()

    # Loop through each file in the folder
    for file_name in os.listdir(image_folder):
        file_path = os.path.join(image_folder, file_name)
        
        if file_name.lower().endswith(('.png', '.jpg')):
            print(f"\nProcessing file: {file_path}")
            image = cv2.imread(file_path)

            # optional preprocessing: i tried it, but the results were not good
            # image = preprocess_image(image)
            edge_detection_output = edge_detection(file_path, image, net, save_path='output')

            # Display the processed image
            cv2.imshow("Processed Image", edge_detection_output)
            key = cv2.waitKey(0)
            cv2.destroyAllWindows()