using OwlLogging;
using Shared;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Client
{
    public class EmptyUiCatcher : MonoBehaviour, IDropHandler
    {
        public static EmptyUiCatcher Instance;

        private class ItemDropProcess
        {
            public long ItemTypeId;
            public int InventoryId;
        }

        private ItemDropProcess _currentItemDropProcess = new();

        void Awake()
        {
            if(Instance != null)
            {
                OwlLogger.LogError("Duplicate EmptyUiCatchers!", GameComponent.UI);
                Destroy(this);
                return;
            }

            Instance = this;
            SetCatcherActive(false);
        }

        public void SetCatcherActive(bool value)
        {
            gameObject.SetActive(value);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null)
                return;

            if(eventData.pointerDrag.TryGetComponent(out ItemStackWidget itemStackWidget))
            {
                // Inventory -> empty UI: Drop Items
                if(itemStackWidget.DragSource == ItemStackDragSource.OwnInventory)
                {
                    _currentItemDropProcess.ItemTypeId = itemStackWidget.CurrentType.TypeId;
                    _currentItemDropProcess.InventoryId = ClientMain.Instance.CurrentCharacterData.InventoryId;

                    ClientMain.Instance.GeneralNumberInput.Show(0, itemStackWidget.CurrentCount,
                        itemStackWidget.CurrentCount, OnItemDropAmountInputConfirm, null);
                }
            }

            SetCatcherActive(false);
        }

        private void OnItemDropAmountInputConfirm(int amount)
        {
            ItemDropRequestPacket packet = new()
            {
                InventoryId = _currentItemDropProcess.InventoryId,
                ItemTypeId = _currentItemDropProcess.ItemTypeId,
                Amount = amount
            };

            ClientMain.Instance.ConnectionToServer.Send(packet);
        }
    }
}

