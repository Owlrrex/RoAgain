using UnityEngine;
using UnityEngine.Rendering.Universal;
using OwlLogging;

[RequireComponent(typeof(Camera))]
public class TemporaryOverlayCamera : MonoBehaviour
{
    private Camera _referenceCamera;
    private bool _registered;

    void Awake()
    {
        _referenceCamera = GetComponent<Camera>();
        if(_referenceCamera == null)
        {
            OwlLogger.LogError("TemporaryOverlayCamera can't find Camera!", GameComponent.Other);
            Destroy(this);
        }
    }

    void OnEnable()
    {
        //
    }

    private void Update()
    {
        if (_registered)
            return;

        if (Camera.main == null)
            return;

        UniversalAdditionalCameraData data = Camera.main.GetUniversalAdditionalCameraData();
        if (data == null
            || data.cameraStack == null)
        {
            OwlLogger.LogError("Camera.main is missing AdditionalCameraData, or has no stack!", GameComponent.Other);
            Destroy(this);
            return;
        }
        
        data.cameraStack.Add(_referenceCamera);
        _registered = true;
    }

    // Update is called once per frame
    void OnDisable()
    {
        if(_registered)
        {
            Camera.main.GetUniversalAdditionalCameraData().cameraStack.Remove(_referenceCamera);
            _registered = false;
        }
    }
}
