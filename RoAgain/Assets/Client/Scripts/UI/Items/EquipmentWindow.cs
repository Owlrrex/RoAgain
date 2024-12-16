using OwlLogging;
using Shared;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Client
{
    public class EquipmentWindow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private GameObject _slotContainer;

        [SerializeField]
        private Button _closeButton;

        private Dictionary<EquipmentSlot, EquipmentSlotWidget> _slotWidgets = new();

        void Awake()
        {
            if(!OwlLogger.PrefabNullCheckAndLog(_slotContainer, nameof(_slotContainer), this, GameComponent.UI))
            {
                _slotWidgets.Clear();
                foreach(EquipmentSlotWidget widget in _slotContainer.GetComponentsInChildren<EquipmentSlotWidget>())
                {
                    _slotWidgets.Add(widget.CurrentSlot, widget);
                    widget.ItemDropped += OnItemDroppedOnSlot;
                }
            }

            if (!OwlLogger.PrefabNullCheckAndLog(_closeButton, nameof(_closeButton), this, GameComponent.UI))
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        public void SetSet(EquipmentSet newSet)
        {
            foreach(var kvp in _slotWidgets)
            {
                kvp.Value.SetEquipmentSet(newSet);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null)
                return;

            if (!eventData.pointerDrag.TryGetComponent(out ItemStackWidget itemWidget))
                return;

            ItemType type = itemWidget.CurrentType;
            if (type is not EquippableItemType equipType)
                return;

            int groupIndex = 0;
            foreach (var kvp in equipType.SlotCriteriums)
            {
                bool slotValid = true;
                if(kvp.Value != null)
                {
                    foreach (IBattleEntityCriterium criterium in kvp.Value)
                    {
                        if (!criterium.Evaluate(ClientMain.Instance.CurrentCharacterData))
                        {
                            slotValid = false;
                            break;
                        }
                    }
                }
                
                if(slotValid)
                {
                    foreach(EquipmentSlot singleSlot in new EquipmentSlotIterator(kvp.Key))
                    {
                        if (!_slotWidgets.ContainsKey(singleSlot))
                            continue;

                        _slotWidgets[singleSlot].SetGroupIndicatorActive(groupIndex, true, kvp.Key);
                    }
                    groupIndex++;
                }
            }
            
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideAllGroupIndicators();
        }

        private void OnCloseClicked()
        {
            gameObject.SetActive(false);
        }

        private void OnItemDroppedOnSlot()
        {
            HideAllGroupIndicators();
        }

        private void HideAllGroupIndicators()
        {
            const int MAX_GROUP_INDEX = 5;
            for (int groupIndex = 0; groupIndex < MAX_GROUP_INDEX; groupIndex++)
            {
                foreach (var kvp in _slotWidgets)
                {
                    kvp.Value.SetGroupIndicatorActive(groupIndex, false, EquipmentSlot.Unknown);
                }
            }
        }
    }
}

