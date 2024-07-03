using OwlLogging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseFollower : MonoBehaviour
{
    [SerializeField]
    private RectTransform _canvasTransform;
    [SerializeField]
    private Camera _referenceCamera;

    private Vector2 localPos;

    private void Awake()
    {
        OwlLogger.PrefabNullCheckAndLog(_canvasTransform, nameof(_canvasTransform), this, GameComponent.UI);
        OwlLogger.PrefabNullCheckAndLog(_referenceCamera, nameof(_referenceCamera), this, GameComponent.UI);
    }

    // Update is called once per frame
    void Update()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasTransform, Input.mousePosition, _referenceCamera, out localPos);
        transform.localPosition = localPos;
    }
}
