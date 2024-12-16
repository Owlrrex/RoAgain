using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    public class EquipmentSlotWidget : MonoBehaviour
    {
        [SerializeField]
        private ItemStackWidget _itemWidget;

        [field: SerializeField]
        public EquipmentSlot CurrentSlot { get; private set; }

        private EquipmentSet _currentSet;

        public Action ItemDropped;

        [SerializeField]
        private List<EquipmentSlotGroupIndicator> _groupIndicators;

        private void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_itemWidget, nameof(_itemWidget), this, GameComponent.UI);
            foreach (EquipmentSlotGroupIndicator indicator in _groupIndicators)
            {
                indicator.ItemDropped += OnItemDropped;
                indicator.gameObject.SetActive(false);
            }
        }

        public void SetData(EquipmentSlot slot, EquipmentSet equipSet)
        {
            SetSlot(slot);
            SetEquipmentSet(equipSet);
        }

        public void SetSlot(EquipmentSlot slot)
        {
            if (CurrentSlot == slot)
                return;

            CurrentSlot = slot;
            DisplayDataIfComplete();
        }

        public void SetEquipmentSet(EquipmentSet equipSet)
        {
            if (_currentSet == equipSet)
                return;

            if(_currentSet != null)
            {
                _currentSet.EquipmentChanged -= OnEquipmentChanged;
            }

            _currentSet = equipSet;

            if(_currentSet != null)
            {
                _currentSet.EquipmentChanged += OnEquipmentChanged;
            }
            DisplayDataIfComplete();
        }

        private void DisplayDataIfComplete()
        {
            if (CurrentSlot == EquipmentSlot.Unknown || _currentSet == null)
                return;

            if(_currentSet.HasItemEquippedInSlot(CurrentSlot))
            {
                EquippableItemType type = _currentSet.GetItemType(CurrentSlot);
                _itemWidget.SetData(type, -1, ItemStackDragSource.OwnEquip);
                _itemWidget.gameObject.SetActive(true);
            }
            else
            {
                _itemWidget.gameObject.SetActive(false);
            }
        }

        private void OnEquipmentChanged(EquipmentSlot slot, EquippableItemType itemType)
        {
            if (CurrentSlot != slot)
                return;

            DisplayDataIfComplete();
        }

        public void SetGroupIndicatorActive(int groupIndex, bool newActive, EquipmentSlot groupedSlots)
        {
            if(groupIndex >= _groupIndicators.Count)
            {
                // TODO: Gracefully handle too-large groupIndexes
                return;
            }

            EquipmentSlotGroupIndicator indicator = _groupIndicators[groupIndex];
            indicator.GroupedSlots = groupedSlots;
            indicator.gameObject.SetActive(newActive);
        }
        
        private void OnItemDropped()
        {
            ItemDropped?.Invoke();
        }
    }
}

