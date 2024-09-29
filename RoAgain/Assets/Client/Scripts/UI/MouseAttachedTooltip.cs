using OwlLogging;
using Shared;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Client
{
    public class MouseAttachedTooltip : MonoBehaviour
    {
        public static MouseAttachedTooltip Instance { get; private set; }

        [SerializeField]
        private RectTransform _canvasTransform;
        [SerializeField]
        private Camera _referenceCamera;
        [SerializeField]
        private TMP_Text _text;

        private Vector2 _localPos;
        private object _tooltipOwner;

        private void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_canvasTransform, nameof(_canvasTransform), this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_referenceCamera, nameof(_referenceCamera), this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_text, nameof(_text), this, GameComponent.UI);

            if(Instance != null)
            {
                OwlLogger.LogError("MouseAttachedTooltip can't register - Instance already present!", GameComponent.UI);
                Destroy(this);
                return;
            }

            Instance = this;
            gameObject.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
            SetToMousePos();
        }

        private void SetToMousePos()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasTransform, Input.mousePosition, _referenceCamera, out _localPos);
            transform.localPosition = _localPos;
        }

        public void Show(string text, object tooltipOwner)
        {
            _text.text = text;
            SetToMousePos();
            gameObject.SetActive(true);
            _tooltipOwner = tooltipOwner;
        }

        public void Show(LocalizedStringId locId, object tooltipOwner)
        {
            if (locId == LocalizedStringId.INVALID)
            {
                Hide();
                return;
            }

            Show(LocalizedStringTable.GetStringById(locId), tooltipOwner);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            _tooltipOwner = null;
        }

        public bool IsMine(object candidate)
        {
            return candidate == _tooltipOwner;
        }

        public void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}