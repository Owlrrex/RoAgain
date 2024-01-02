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
            Target = PlayerMain.Instance.UiCamera.transform;
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
        transform.LookAt(Target.position);
    }
}
