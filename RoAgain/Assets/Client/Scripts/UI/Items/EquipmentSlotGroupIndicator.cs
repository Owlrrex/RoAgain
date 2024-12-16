using OwlLogging;
using Shared;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Client
{
    public class EquipmentSlotGroupIndicator : MonoBehaviour, IDropHandler
    {
        public EquipmentSlot GroupedSlots; // Needed to send accurate equip-request

        public Action ItemDropped;

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null
                || !eventData.pointerDrag.TryGetComponent(out ItemStackWidget itemWidget))
                return;

            if (GroupedSlots == EquipmentSlot.Unknown)
            {
                OwlLogger.LogError("Can't attempt to equip item into unknown EquipmentSlot!", GameComponent.UI);
                return;
            }

            EquipRequestPacket packet = new()
            {
                ItemTypeId = itemWidget.CurrentType.TypeId,
                OwnerEntityId = ClientMain.Instance.CurrentCharacterData.Id,
                Slot = GroupedSlots
            };
            ClientMain.Instance.ConnectionToServer.Send(packet);

            ItemDropped?.Invoke();
        }
    }
}

