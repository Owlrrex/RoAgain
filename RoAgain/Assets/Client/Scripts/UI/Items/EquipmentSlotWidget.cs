using OwlLogging;
using Shared;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Client
{
    public class EquipmentSlotWidget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private ItemStackWidget _itemWidget;
        [SerializeField]
        private GameObject _hoverHighlight;

        [field: SerializeField]
        public EquipmentSlot CurrentSlot { get; private set; }

        private EquipmentSet _currentSet;

        private void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_itemWidget, nameof(_itemWidget), this, GameComponent.UI);
            if (!OwlLogger.PrefabNullCheckAndLog(_hoverHighlight, nameof(_hoverHighlight), this, GameComponent.UI))
                _hoverHighlight.SetActive(false);
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

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null)
                return;

            if (eventData.pointerDrag.TryGetComponent(out ItemStackWidget itemStackWidget))
            {
                // TODO: Try to equip item
            }

            _hoverHighlight.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!eventData.dragging
                || eventData.pointerDrag == null)
                return;

            if (eventData.pointerDrag.TryGetComponent(out ItemStackWidget itemStackWidget))
            {
                // TODO: Check more detailed equippability conditions & determining which slots to highlight
                _hoverHighlight.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hoverHighlight.SetActive(false);
        }
    }
}

