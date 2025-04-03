using System.Collections;
using MagicLeap.OpenXR.Subsystems;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.Management;

public class PlaneConfigurationManager : MonoBehaviour
{
    [SerializeField] private uint maxResults = 100;

    [SerializeField] private float minPlaneArea = 0.25f;

    // private readonly MLPermissions.Callbacks permissionsCallbacks = new();

    private ARPlaneManager planeManager;
    
    private bool permissionGranted = false;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        planeManager = FindObjectOfType<ARPlaneManager>();
        // permissionsCallbacks.OnPermissionGranted += OnPermissionGranted;
        // permissionsCallbacks.OnPermissionDenied += OnPermissionDenied;
        // permissionsCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
    }

    private void OnDestroy()
    {
        // permissionsCallbacks.OnPermissionGranted -= OnPermissionGranted;
        // permissionsCallbacks.OnPermissionDenied -= OnPermissionDenied;
        // permissionsCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(AreSubsystemsLoaded);
        MagicLeap.Android.Permissions.RequestPermission(MagicLeap.Android.Permissions.SpatialMapping, 
        OnPermissionDenied, OnPermissionGranted, OnPermissionDenied);
    }

    private bool AreSubsystemsLoaded()
    {
        if (XRGeneralSettings.Instance == null && XRGeneralSettings.Instance.Manager == null) return false;
        var activeLoader = XRGeneralSettings.Instance.Manager.activeLoader;
        if (activeLoader == null) return false;
        return activeLoader.GetLoadedSubsystem<XRPlaneSubsystem>() != null;
    }

    private void OnPermissionGranted(string permission)
    {
        // LearnXR.Core.Logger.Instance.LogInfo($"Permission {permission} was granted");
        planeManager.enabled = true;
        permissionGranted = true;
    }

    private void OnPermissionDenied(string permission)
    {
        // LearnXR.Core.Logger.Instance.LogInfo($"Permission {permission} was denied");
        planeManager.enabled = false;
    }

    private void Update() => UpdateQuery();

    private void UpdateQuery()
    {
        if (planeManager != null && planeManager.enabled && permissionGranted)
        {
      
            var newQuery = new MLXrPlaneSubsystem.PlanesQuery
            {
                Flags = planeManager.requestedDetectionMode.ToMLXrQueryFlags() | MLXrPlaneSubsystem.MLPlanesQueryFlags.SemanticAll,
                BoundsCenter = mainCamera.transform.position,
                BoundsRotation = mainCamera.transform.rotation,
                BoundsExtents = Vector3.one * 20f,
                MaxResults = maxResults,
                MinPlaneArea = minPlaneArea
            };

            MLXrPlaneSubsystem.Query = newQuery;

            MagicLeap.OpenXR.Subsystems.MLXrPlaneSubsystem.Query = newQuery;
        }
    }
}
