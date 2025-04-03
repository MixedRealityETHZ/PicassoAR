using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.MagicLeap;

public class MarkerTracker : MonoBehaviour
{
    [Tooltip("Canvas to spawn at detected marker positions.")]
    public GameObject CanvasPrefab;

    [Tooltip("Size of detected QR markers (in meters).")]
    public float QrCodeMarkerSize = 0.05f;

    [Tooltip("Size of detected ArUco markers (in meters).")]
    public float ArucoMarkerSize = 0.05f;

    [Tooltip("Type of marker to detect (QR, ArUco, etc.).")]
    public MLMarkerTracker.MarkerType Type = MLMarkerTracker.MarkerType.QR;

    [Tooltip("ArUco dictionary used for detection.")]
    public MLMarkerTracker.ArucoDictionaryName ArucoDict = MLMarkerTracker.ArucoDictionaryName.DICT_5X5_100;

    [Tooltip("Tracking profile for marker detection.")]
    public MLMarkerTracker.Profile Profile = MLMarkerTracker.Profile.Default;

    // Dictionary to store marker instances keyed by their ID.
    private Dictionary<string, GameObject> _markers = new Dictionary<string, GameObject>();

    // Used to decode binary data from detected markers into strings.
    private ASCIIEncoding _asciiEncoder = new ASCIIEncoding();
    public UnityEvent OnMarkerLocalized;
    public GameObject detectedMarker;

#if UNITY_ANDROID
    private void OnEnable()
    {
        // Subscribe to the event that fires when a marker is detected.
        MLMarkerTracker.OnMLMarkerTrackerResultsFound += OnTrackerResultsFound;
    }

    private void Start()
    {
        // Create and apply settings for marker tracking.
        MLMarkerTracker.TrackerSettings trackerSettings = MLMarkerTracker.TrackerSettings.Create(
            true,
            Type,
            QrCodeMarkerSize,
            ArucoDict,
            ArucoMarkerSize,
            Profile
        );
        _ = MLMarkerTracker.SetSettingsAsync(trackerSettings);
    }

    private void OnDisable()
    {
        // Unsubscribe from the detection event when disabled.
        MLMarkerTracker.OnMLMarkerTrackerResultsFound -= OnTrackerResultsFound;
        _ = MLMarkerTracker.StopScanningAsync();
    }

    private void OnTrackerResultsFound(MLMarkerTracker.MarkerData data)
    {
        string id = "";
        float markerSize = 0.01f;

        // Determine marker type and extract its ID and size.
        switch (data.Type)
        {
            case MLMarkerTracker.MarkerType.Aruco_April:
                id = data.ArucoData.Id.ToString();
                markerSize = ArucoMarkerSize;
                break;
            case MLMarkerTracker.MarkerType.QR:
                id = _asciiEncoder.GetString(data.BinaryData.Data, 0, data.BinaryData.Data.Length);
                markerSize = QrCodeMarkerSize;
                break;
            case MLMarkerTracker.MarkerType.EAN_13:
            case MLMarkerTracker.MarkerType.UPC_A:
                id = _asciiEncoder.GetString(data.BinaryData.Data, 0, data.BinaryData.Data.Length);
                Debug.Log("No pose for " + data.Type + " marker, value: " + data.BinaryData.Data);
                break;
        }

        // If we have a valid ID, either update existing marker or create a new one.
        Debug.Log("marker id: " + id);
        if (!string.IsNullOrEmpty(id))
        {
            if (_markers.ContainsKey(id))
            {
                // Update position and rotation of existing marker.
                GameObject marker = _markers[id];
                marker.transform.position = data.Pose.position;
                marker.transform.rotation = data.Pose.rotation;
                detectedMarker = marker;
            }
            else
            {
                // Instantiate a new marker at the detected pose.
                GameObject marker = Instantiate(CanvasPrefab, data.Pose.position, data.Pose.rotation);
                marker.transform.localScale = new Vector3(markerSize, markerSize, markerSize);
                _markers.Add(id, marker);
                detectedMarker = marker;
            }
            Debug.Log("Marker found: " + detectedMarker + "at pos: " + detectedMarker.transform.position);
            OnMarkerLocalized.Invoke();
        }
    }
#endif
}