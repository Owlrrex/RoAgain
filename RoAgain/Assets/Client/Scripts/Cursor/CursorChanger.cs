using OwlLogging;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    public class CursorChanger : MonoBehaviour
    {
        public enum HoverTargetType
        {
            Unknown,
            Normal,
            Attack,
            Warp,
            Speak
        }

        [Serializable]
        public struct CursorEntry
        {
            public Texture2D texture;
            public Vector2 hotspot;
        }

        [SerializeField]
        private CursorEntry _normalCursor;
        [SerializeField]
        private CursorEntry _attackCursor;
        [SerializeField]
        private CursorEntry _warpCursor;
        // TODO: Speak cursor

        [SerializeField]
        private Camera _camera;
        [SerializeField]
        private LayerMask _hoverableLayers;

        private HoverTargetType _lastType = HoverTargetType.Unknown;

        // Start is called before the first frame update
        void Start()
        {
            if (_normalCursor.texture == null
                || _attackCursor.texture == null
                || _warpCursor.texture == null)
            {
                OwlLogger.LogError($"CursorChanger doesn't have all cursor textures provided!", GameComponent.Other);
                Destroy(this);
            }
        }

        // Update is called once per frame
        void Update()
        {
            HoverTargetType type = GetHoverTargetType();
            if (type == _lastType)
                return;

            _lastType = type;

            switch (type)
            {
                case HoverTargetType.Attack:
                    Cursor.SetCursor(_attackCursor.texture, _attackCursor.hotspot, CursorMode.Auto);
                    break;
                case HoverTargetType.Warp:
                    Cursor.SetCursor(_warpCursor.texture, _warpCursor.hotspot, CursorMode.Auto);
                    break;
                case HoverTargetType.Speak:
                default:
                    Cursor.SetCursor(_normalCursor.texture, _normalCursor.hotspot, CursorMode.Auto);
                    break;
            }
        }

        private HoverTargetType GetHoverTargetType()
        {
            Vector3 mousePos = Input.mousePosition;
            RaycastHit hit;
            Ray ray = _camera.ScreenPointToRay(mousePos);
            if (!Physics.Raycast(ray, out hit, 100, _hoverableLayers))
                return HoverTargetType.Unknown;

            GameObject hoveredObject = hit.transform.gameObject;
            if (hoveredObject == null)
                return HoverTargetType.Unknown;

            CursorModifierComponent comp = hoveredObject.GetComponent<CursorModifierComponent>();
            if (comp == null)
                return HoverTargetType.Unknown;

            return comp.TargetType;
        }
    }
}