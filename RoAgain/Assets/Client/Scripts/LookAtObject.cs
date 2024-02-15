using OwlLogging;
using UnityEngine;

public class LookAtObject : MonoBehaviour
{
    [SerializeField]
    private bool _usePlayerUiCamera;

    public Transform Target;
    // Start is called before the first frame update
    void Start()
    {
        if(_usePlayerUiCamera)
        {
            Target = PlayerMain.Instance.WorldUiCamera.transform;
        }

        if (OwlLogger.PrefabNullCheckAndLog(Target, "Target", this, GameComponent.Other))
        {
            Destroy(this);
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_usePlayerUiCamera)
        {
            // Special aligning method that avoids perspective skew
            if (transform.rotation != PlayerMain.Instance.UiCanvasNonSkewRotation)            
                transform.rotation = PlayerMain.Instance.UiCanvasNonSkewRotation;
        }
        else
        {
            transform.LookAt(Target.position);
        }
        
    }
}
