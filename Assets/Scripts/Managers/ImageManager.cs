using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using System.Collections;
using System;
using MixedReality.Toolkit.UX;
using static UnityEngine.XR.MagicLeap.MLCameraBase.Metadata;

using PicassoAR.Utils;
using UnityEngine.Android;
using Unity.VisualScripting;
using MagicLeap.OpenXR;
using UnityEngine.Events;

public class ImageManager : MonoBehaviour
{
#region Unity Settings
    [Header("File System Settings")]
    public string defaultDirectory = "/storage/self/primary/DCIM/photos/";
    public string imageFile = "example.jpg";

    [Header("Image Fetcher Settings")]
    public ImageFetcher fetcher;
    public string imageName;

    [Header("UI Elements")]
    private PressableButton loadFromFileButton;
    private PressableButton captureImageButton;

    [Header("Panels")]
    [SerializeField, Tooltip("select or capture image")]
    public GameObject imageSelectionButtons;
    [SerializeField, Tooltip("use the captured image or capture again")]
    public GameObject captureOptionsButtons;
    [SerializeField, Tooltip("image fetch options")]
    public GameObject fetchOptionsButtons;

    private PressableButton useImageButtonCaption;
    private PressableButton useImageButtonSelect;
    private PressableButton useImageButtonFetch;

    [SerializeField]
    public Texture2D imageTexture;
#endregion 

#region MLCamera Settings
    [Header("Camera Properties")]
    [SerializeField]
    public int cameraWidth;
    [SerializeField]
    public int cameraHeight;

    private string[] permissions = new String[]{
            Permission.Camera, Permission.ExternalStorageRead // MLPermission
    };

    [Header("MLCamera Settings")]
    [SerializeField, Tooltip("The renderer to show the camera capture on JPG format")]
    private RawImage _screenRendererJPEG = null;
    private MLCamera colorCamera;
    private bool _permissionGRanted = true;
    private bool _isCameraConnected;
    private bool _cameraDeviceAvailable;
    private bool _isCapturingImage;
#endregion

#region ImageServer
    public ImageServer server;
#endregion

#region GlobalState
    public UnityEvent OnImageSelected;
#endregion

#region Unity Routine
    void OnEnable()
    {   
        // Set status
        // selectingImage = true;

        // Set button group visibility
        imageSelectionButtons.SetActive(true);
        captureOptionsButtons.SetActive(false);
        fetchOptionsButtons.SetActive(false);
        fetchOptionsButtons.SetActive(false);
        useImageButtonSelect = Helpers.GetChildComponentByName<PressableButton>(imageSelectionButtons, "UseImageButton");
        useImageButtonCaption = Helpers.GetChildComponentByName<PressableButton>(captureOptionsButtons, "UseImageButton");
        useImageButtonFetch = Helpers.GetChildComponentByName<PressableButton>(fetchOptionsButtons, "UseImageButton");
        useImageButtonCaption.enabled = false;
        useImageButtonSelect.enabled = false;
        useImageButtonFetch.enabled = false;

        // Ask for permissions
        MagicLeap.Android.Permissions.RequestPermissions(
            permissions, 
            OnPermissionGranted, OnPermissionDenied, OnPermissionDeniedAndDontAskAgain
        );

        if(_permissionGRanted)
        {
            Debug.Log("All permissions granted, entering application");
        }
        else 
        {
            Debug.Log("At least one permission was not granted, check your settings!");
        }
    }

    void Update()
    {
        if(imageTexture != null)
        {
            if(imageSelectionButtons.activeSelf)
            {
                useImageButtonSelect.enabled = true;
            } else if (captureOptionsButtons.activeSelf)
            {
                useImageButtonCaption.enabled = true;
            } else if (fetchOptionsButtons.activeSelf) 
            {
                useImageButtonFetch.enabled = true;    
            }
        } else {
            if(useImageButtonSelect.enabled)
            {
                useImageButtonSelect.enabled = false;

            } else if (useImageButtonCaption.enabled)
            {
                useImageButtonCaption.enabled = false;
            } else if (useImageButtonFetch.enabled)
            {
                useImageButtonFetch.enabled = false;
            }
        }
    }

    private void OnPermissionDenied(string permission)
    {
        _permissionGRanted &= false;
        Debug.Log($"Permission {permission} denied, please check your settings.");
    }

    private void OnPermissionGranted(string permission)
    {
        _permissionGRanted &= true;
        Debug.Log($"{permission} granted.");
    }

    private void OnPermissionDeniedAndDontAskAgain(string permission)
    {
        Debug.Log($"{permission} was denied and cannot be request again.");
    }
#endregion

#region Manager Methods
    public void OnLoadImageClicked()
    {
        Texture2D loadedTexture = FileSystemUtils.LoadImageFromPath(path: defaultDirectory + imageFile);
        if(loadedTexture == null){
            Debug.Log("Load image failed");
        } else
        {
            imageTexture = loadedTexture;
            Helpers.UpdateAndApplyImageTexture(ref _screenRendererJPEG, imageTexture);
            // _screenRendererJPEG.material.mainTexture = imageTexture;
            // _screenRendererJPEG.texture = imageTexture;
            // imageTexture.Apply();
        }
    }

    public void OnFetchImageClicked()
    {   
        imageSelectionButtons.SetActive(false);
        fetchOptionsButtons.SetActive(true);
    }


    public void OnSendRawImage()
    {
        // Send the screenRender image texture over the network and let the server process it
        // ensure that the image texture is present
        // TODO; this has to come with a path string which the user selected, it also needs an UI attached to it
        if(imageTexture == null)
        {   
            Debug.Log("Cannot send image to server. ImageTexture not found!");
        } else
        {   
            StartCoroutine(server.SendImageToServer(texture: imageTexture));
        }
    }

    public void OnUseRawImageFromServer()
    {
        // TODO: select a raw image from the server and let it to be processed
        StartCoroutine(server.FetchProcessedImage(imgName: imageName, result => { 
                imageTexture = result;
                Helpers.UpdateAndApplyImageTexture(ref _screenRendererJPEG, imageTexture);
                Debug.Log("Received raw image.");
        } )); 
    }

    public void OnUseProcessedImageFromServer()
    {
        // TODO: fetch a processed image from the server display it on the image texture
        StartCoroutine(server.FetchProcessedImage(imgName: imageName, result => { 
                imageTexture = result;
                Helpers.UpdateAndApplyImageTexture(ref _screenRendererJPEG, imageTexture);
                Debug.Log("Received processed image.");
        } )); 
    }


    public void OnCaptureImageClicked()
    {
        imageSelectionButtons.SetActive(false);
        CaptureImageRoutine();
    }

    public void OnQuitClicked()
    {
        Debug.Log("Quit application.");
        Application.Quit();
    }

    public void OnAgainClicked()
    {
        captureOptionsButtons.SetActive(false);
        CaptureImageRoutine();
    }

    public void OnUseImageClicked()
    {
        Debug.Log("Using captured or loaded image.");
        // draggableImageObject.GetComponent<Renderer>().material.mainTexture = currentImageTexture;
        // draggableImageObject.SetActive(true);
        if(captureOptionsButtons.activeSelf)
        {
            captureOptionsButtons.SetActive(false);
        }
        if(imageSelectionButtons.activeSelf)
        {
            imageSelectionButtons.SetActive(false);
        }
        if(fetchOptionsButtons.activeSelf)
        {
            fetchOptionsButtons.SetActive(false);
        }

        // selectingImage = false;
        OnImageSelected?.Invoke();
    }
#endregion 


#region Helper Methods

    private void CaptureImageRoutine()
    {
        if(!_permissionGRanted)
        {
            MLPluginLog.Warning("Unable to capture image. Camera permission denied.");
        } else
        {
            // capture an image and preview it on the canvas
            // https://developer-docs.magicleap.cloud/docs/guides/unity/camera/ml-camera-example/
            StartCoroutine(EnableMLCamera());
            StartCoroutine(CaptureImagesLoop());

            // ask the user to use it or to capture again
            captureOptionsButtons.SetActive(true);
        }
    }

    private IEnumerator EnableMLCamera()
    {
        // Loop until the camera device is available
        while (!_cameraDeviceAvailable)
        {
            MLResult result = MLCamera.GetDeviceAvailabilityStatus(MLCamera.Identifier.Main, out _cameraDeviceAvailable);
            if (!(result.IsOk && _cameraDeviceAvailable))
            {
                // Wait until camera device is available
                yield return new WaitForSeconds(1.0f);
            }
        }

        Debug.Log("Camera device available.");

        // Create and connect the camera with a context that enables video stabilization and camera only capture
        ConnectCamera();

        // Wait until the camera is connected since this script uses the async "CreateAndConnectAsync" Method to connect to the camera.
        while (!_isCameraConnected)
        {
            yield return null;
        }

        Debug.Log("Camera device connected.");

        // Prepare the camera for capture with a configuration that specifies JPEG output format, frame rate, and resolution
        ConfigureAndPrepareCapture();
    }

    // Define a coroutine that captures an image if the camera is connected and supports image capture type. 
    // The image is then captured async
    private IEnumerator CaptureImagesLoop()
    {
        // while (true)
        // {
            if (_isCameraConnected && !_isCapturingImage)
            {
                if (MLCamera.IsCaptureTypeSupported(colorCamera, MLCamera.CaptureType.Image))
                {
                    CaptureImage(); //
                }
            }
            yield return new WaitForSeconds(0.5f);
        // }
    }


    // Define an async method that will create and connect the camera with a context that enables video stabilization and Video only capture
    private async void ConnectCamera()
    {
        MLCamera.ConnectContext context = MLCamera.ConnectContext.Create();
        context.EnableVideoStabilization = false;
        context.Flags = MLCameraBase.ConnectFlag.CamOnly;

        // Use the CreateAndConnectAsync method to create and connect the camera asynchronously
        colorCamera = await MLCamera.CreateAndConnectAsync(context);

        if (colorCamera != null)
        {
            // Register a callback for when a raw image is available after capture
            colorCamera.OnRawImageAvailable += OnCaptureRawImageComplete;
            _isCameraConnected = true;
        }
    }


    // Define an async method that will prepare the camera for capture with a configuration that specifies
    // JPEG output format, frame rate, and resolution
    private async void ConfigureAndPrepareCapture()
    {
        MLCamera.CaptureStreamConfig[] imageConfig = new MLCamera.CaptureStreamConfig[1]
        {
            new MLCamera.CaptureStreamConfig()
            {
                OutputFormat = MLCamera.OutputFormat.JPEG,
                CaptureType = MLCamera.CaptureType.Image,
                Width = cameraWidth,
                Height = cameraHeight
            }
        };

        MLCamera.CaptureConfig captureConfig = new MLCamera.CaptureConfig()
        {
            StreamConfigs = imageConfig,
            CaptureFrameRate = MLCamera.CaptureFrameRate._30FPS
        };

        // Use the PrepareCapture method to set the capture configuration and get the metadata handle
        MLResult prepareCaptureResult = colorCamera.PrepareCapture(captureConfig, out MLCamera.Metadata _);

        if (!prepareCaptureResult.IsOk)
        {
            return;
        }
    }



    /// <summary>
    /// Takes a picture async with the device's camera using the camera's CaptureImageAsync method.
    /// </summary>
    private async void CaptureImage()
    {
        // Set the flag to indicate that an image capture is in progress
        _isCapturingImage = true;

        var aeawbResult = await colorCamera.PreCaptureAEAWBAsync();
        if (!aeawbResult.IsOk)
        {
            Debug.LogError("Image capture failed!");
        }
        else
        {
            var result = await colorCamera.CaptureImageAsync(1);
            if (!result.IsOk)
            {
                Debug.LogError("Image capture failed!");
            }
        }

        // Reset the flag to indicate that image capture is complete
        _isCapturingImage = false;
    }

    /// <summary>
    /// Handles the event of a new image getting captured and visualizes it with the Image Visualizer
    /// </summary>
    /// <param name="capturedImage">Captured frame.</param>
    /// <param name="resultExtras">Results Extras.</param>
    private void OnCaptureRawImageComplete(MLCamera.CameraOutput capturedImage, MLCamera.ResultExtras resultExtras, MLCamera.Metadata metadataHandle)
    {
        MLResult aeStateResult = metadataHandle.GetControlAEStateResultMetadata(out ControlAEState controlAEState);
        MLResult awbStateResult = metadataHandle.GetControlAWBStateResultMetadata(out ControlAWBState controlAWBState);

        if (aeStateResult.IsOk && awbStateResult.IsOk)
        {
            bool autoExposureComplete = controlAEState == MLCameraBase.Metadata.ControlAEState.Converged || controlAEState == MLCameraBase.Metadata.ControlAEState.Locked;
            bool autoWhiteBalanceComplete = controlAWBState == MLCameraBase.Metadata.ControlAWBState.Converged || controlAWBState == MLCameraBase.Metadata.ControlAWBState.Locked;

            if (autoExposureComplete && autoWhiteBalanceComplete)
            {
                // This example is configured to render JPEG images only. // TODO: include other formats
                if(capturedImage.Format == MLCameraBase.OutputFormat.JPEG)
                {
                    UpdateJPGTexture(capturedImage.Planes[0], ref _screenRendererJPEG);
                }
            }
        }

    }

    private void UpdateJPGTexture(MLCamera.PlaneInfo imagePlane, ref RawImage renderer)
    {
        if (imageTexture != null)
        {
            Destroy(imageTexture);
        }
        int x = cameraWidth;
        int y = cameraHeight;

        imageTexture = new Texture2D(x, y);
        bool status = imageTexture.LoadImage(imagePlane.Data);

        if (status && (imageTexture.width != x && imageTexture.height != y))
        {
            renderer.material.mainTexture = imageTexture;
            renderer.texture = imageTexture;
            imageTexture.Apply();
        }
    }
}
#endregion

// Helper component for draggable behavior
// TODO
public class DraggableObject : MonoBehaviour
{
    private bool isFrozen = false;

    void Update()
    {
        if (isFrozen) return;

        // Implement drag, rotate, and scale interactions (e.g., using ML Gestures or touch input)
    }

    public void FreezeObject()
    {
        isFrozen = true;
    }
}
