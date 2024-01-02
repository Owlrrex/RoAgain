using OwlLogging;
using System;
using UnityEngine;

namespace Client
{
    public class CameraAnchorDelay : MonoBehaviour
    {
        [SerializeField]
        private Transform _cameraAnchor;
        [SerializeField]
        private Transform _referenceObject;
        [SerializeField]
        private int _delayFrames = 3;

        private Vector3[] _framePositions;

        // Start is called before the first frame update
        void Start()
        {
            if(OwlLogger.PrefabNullCheckAndLog(_cameraAnchor, "cameraAnchor", this, GameComponent.Other))
            {
                Destroy(this);
                return;
            }

            if(OwlLogger.PrefabNullCheckAndLog(_referenceObject, "referenceObject", this, GameComponent.Other))
            {
                Destroy(this);
                return;
            }

            if (_delayFrames <= 0)
            {
                OwlLogger.LogError("Can't delay by less than 1 frames!", GameComponent.Other);
                Destroy(this);
                return;
            }

            _framePositions = new Vector3[_delayFrames];
            Array.Fill(_framePositions, _referenceObject.position);
        }

        // Update is called once per frame
        void Update()
        {
            int i = _delayFrames - 1;
            _cameraAnchor.position = _framePositions[i];
            for (; i >= 1; i--)
            {
                _framePositions[i] = _framePositions[i - 1];
            }
            _framePositions[0] = _referenceObject.position;
        }
    }
}