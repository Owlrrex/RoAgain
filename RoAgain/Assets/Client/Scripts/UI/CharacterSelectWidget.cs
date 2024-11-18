using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OwlLogging;
using System;
using UnityEngine.EventSystems;
using Shared;

namespace Client
{
    public class CharacterSelectWidget : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField]
        private TMP_Text _charNameText;

        [SerializeField]
        private Image _jobIcon;

        [SerializeField]
        private GameObject _charVisual;

        public Action<CharacterSelectWidget> OnWidgetClicked;

        private bool _selected = false;

        [SerializeField]
        private Image _selectionFrame;

        // Start is called before the first frame update
        void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_charNameText, "charNameText", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_jobIcon, "jobIcon", this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_charVisual, "charVisual", this, GameComponent.UI);
            if(!OwlLogger.PrefabNullCheckAndLog(_selectionFrame, "selectionFrame", this, GameComponent.UI))
                _selectionFrame.gameObject.SetActive(false);
        }

        public void SetCharacterData(CharacterSelectionData data)
        {
            if(data == null)
            {
                OwlLogger.LogError("Can't initialize CharacterSelectWidget with null data", GameComponent.UI);
                return;
            }

            _charNameText.text = data.Name;
            // TODO: Icon & Visuals
        }

        public void SetSelected(bool newSelected)
        {
            if (newSelected == _selected)
                return;

            _selected = newSelected;
            UpdateSelectedVisuals();
        }

        private void UpdateSelectedVisuals()
        {
            _selectionFrame.gameObject.SetActive(_selected);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnWidgetClicked?.Invoke(this);
        }
    }
}

