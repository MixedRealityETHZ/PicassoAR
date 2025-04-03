# PicassoAR
Repository for the Mixed Reality Project (HS2024) at ETHZ   

The project aims at simplifying the process of learning to draw by providing real-time guidance through a Magic Leap 2 (ML2) headset. 
Our system transforms user-selected or captured images into a series of stepwise outlines, which are then projected onto a physical surface for tracing.
To achieve a stable overlay, PicassoAR leverages markerbased tracking and continuously updates the projected geometry as users move around the scene. The most compute-intensive vision tasks are offloaded to a lightweight server, ensuring that the ML2 retains sufficient resources for smooth rendering and user interaction. 

## Setup
### MagicLeap2 using Unity
This project uses `Magic Leap MRTK3 1.2.0`, `MLSDK 2.5.0`, `OpenXR Plugin 1.12.1` and runs on `Unity 2022.3.47f1`

### Image Server
The app uses an image server to send data for processing. The processed image will be sent back to the ML2 device afterwards. The image server is a minimalistic python Flask server. Before deploying the Unity project, make sure to do the following:
 - Both the ML2 device and the server is connected to WiFi and are able to communicate with each other. E.g. avoid using connections with AP isolation like personal hotspots.
 - In the scene explorer, find the `ImageServer` object and set its server url to the server's ip address with the correct port (80).
 - To run the server, install the requirements in `src/requirements.txt` inside a virtualenv and start the server by running `python -m app` . Once started, you can upload and view images locally. The images are categorized into "raw" and "processed".

Once the above is done, you should be ready to deploy the app on ML2. There are also ready-to-use images in `src/images/processed` which can be fetched directly.

## Usage
Before using the app, be sure to prepare a marker for localization (QR, AruCo...) and place the marker on, for example, a sheet of paper.

Currently, we only support fetching images from the server. The current image is hard-coded for testing (see `ImageManager.OnUseProcessedImageFromServer()`). To get an image, choose "Fetch Image" > "Use Processed Image". Wait for about a second, the image will be displayed on the canvas. Select the image for drawing by clicking "Use Image". The UI panel for image selection will then disappear, and the marker tracker starts.

Once the marker is detected, the image will be transformed to align with the marker plane. The user can then choose their preferred drawing medium to create artwork based on the reference image.

## Authors
Haleema Ramzan  
Han Xi  
Alen Bisenovic  
