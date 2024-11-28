using OwlLogging;
using Shared;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    public class EquipmentWindow : MonoBehaviour
    {
        [SerializeField]
        private GameObject _slotContainer;

        private Dictionary<EquipmentSlot, EquipmentSlotWidget> _slotWidgets = new();

        void Awake()
        {
            if(!OwlLogger.PrefabNullCheckAndLog(_slotContainer, nameof(_slotContainer), this, GameComponent.UI))
            {
                _slotWidgets.Clear();
                foreach(EquipmentSlotWidget widget in _slotContainer.GetComponentsInChildren<EquipmentSlotWidget>())
                {
                    _slotWidgets.Add(widget.CurrentSlot, widget);
                }
            }
        }

        public void SetSet(EquipmentSet newSet)
        {
            foreach(var kvp in _slotWidgets)
            {
                kvp.Value.SetEquipmentSet(newSet);
            }
        }
    }
}

