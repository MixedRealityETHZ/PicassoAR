using System;
using Unity.XR.CoreUtils;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public ImageManager imageManager;
    public MarkerTracker markerTracker;
    public GameObject imageProjectionCanvas;
    public GameObject canvasControlPanel; // TODO: Manages user adjustment of canvas properties
    public GameControlButtons gameControlButtons; // TODO: add buttons for quitting the game
    // private PressableButton exitButton;
    // private PressableButton againButton;
    private string _imgCanvasName = "ImageProjectionCanvas";

    private enum AppState
    {
        SelectingImage,
        Drawing
    }
    private AppState _currentState;

    void Start()
    {
        InitializeStates();
        imageProjectionCanvas = GameObject.Find(_imgCanvasName);
        imageProjectionCanvas.GetComponent<BoxCollider>().enabled = false;
    }

    void OnDestroy()
    {
        imageManager.OnImageSelected.RemoveAllListeners();
        markerTracker.OnMarkerLocalized.RemoveAllListeners();
        gameControlButtons.OnFixImagePosition.RemoveAllListeners();
        gameControlButtons.OnRelocalizeImage.RemoveAllListeners();
    }


#region Private Methods
    private void SetState(AppState newState)
    {
        if (_currentState == newState) return;

        ExitState(_currentState);
        _currentState = newState;
        EnterState(newState);
    }

    private void EnterState(AppState state)
    {
        switch (state)
        {
            case AppState.SelectingImage:
                imageManager.enabled = true;
                break;
            case AppState.Drawing:
                markerTracker.enabled = true;
                // canvasControlPanel.enabled = true;
                break;
        }
    }

    private void ExitState(AppState state)
    {
        switch (state)
        {
            case AppState.SelectingImage:
                imageManager.enabled = false;
                break;
            case AppState.Drawing:
                markerTracker.enabled = false;
                // canvasControlPanel.enabled = false;
                break;
        }
    }

    private void OnImageSelected()
    {
        imageManager.enabled = false;
        Debug.Log("Image selected. Transitioning to Drawing state.");
        SetState(AppState.Drawing);
        markerTracker.enabled = true;
    }

    private void OnMarkerLocalized()
    {
        markerTracker.enabled = false;
        Debug.Log("Marker localized. Adjusting canvas.");

        // transform image canvas onto the canvas plane 
        GameObject marker = markerTracker.detectedMarker;
        Vector3 markerPos = marker.transform.position;
        Quaternion markerRot = marker.transform.rotation;

        Vector3 canvasPos = imageProjectionCanvas.transform.localPosition;
        Quaternion canvasRot = imageProjectionCanvas.transform.localRotation;
        Vector3 canvasScale = imageProjectionCanvas.transform.localScale;
        imageProjectionCanvas.transform.position = canvasPos + markerPos - new Vector3(0, 0, 0.6f);  //canvasPos + imgCanvas.transform.InverseTransformPoint(markerPos);
        imageProjectionCanvas.transform.rotation = markerRot * canvasRot;

        // TODO: invoke drawing coroutines...
        // move canvas on plane until fix
        gameControlButtons.fixImageButton.SetActive(true);
        imageProjectionCanvas.GetComponent<BoxCollider>().enabled = true;
    }

    private void OnRelocalizeImage()
    {
        // relocalize until code found and canvas fixed on plane
        markerTracker.enabled = true;
    }

    private void OnFixImagePosition()
    {   
        // disable moving the image
        // the markerTracker should already be disabled at this point
        imageProjectionCanvas.GetComponent<BoxCollider>().enabled = false;
    }

    private void InitializeStates()
    {   
        try {
            if(imageManager == null){
                imageManager = FindObjectOfType<ImageManager>();
            }
            if (markerTracker == null)
            {
                markerTracker = FindObjectOfType<MarkerTracker>();
            }
            if(gameControlButtons == null)
            {
                gameControlButtons = FindObjectOfType<GameControlButtons>();
            }
            imageManager.enabled = true;
            markerTracker.enabled = false;
            gameControlButtons.enabled = true;
            // canvasControlPanel.enabled = false;

            imageManager.OnImageSelected.AddListener(OnImageSelected); 
            markerTracker.OnMarkerLocalized.AddListener(OnMarkerLocalized); 
            gameControlButtons.OnFixImagePosition.AddListener(OnFixImagePosition);
            gameControlButtons.OnRelocalizeImage.AddListener(OnRelocalizeImage);

        } catch (Exception e)
        {
            Debug.LogError("Error in initializaton: " + e.Message);
        }
    }
#endregion
}
