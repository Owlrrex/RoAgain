using OwlLogging;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class InventoryWindow : MonoBehaviour
    {
        public ItemStackDragSource DragSource;

        [SerializeField]
        private Transform _itemContainer;
        [SerializeField]
        private GameObject _itemStackWidgetPrefab;
        [SerializeField]
        private InventoryFilterTabGroup _filterGroup;
        [SerializeField]
        private Button _closeButton;

        private InventoryFilter _currentInventoryFilter;

        private Dictionary<long, ItemStackWidget> _createdItemWidgets = new();

        private Inventory _currentInventory;

        public void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_itemContainer, nameof(_itemContainer), this, GameComponent.UI);
            if (!OwlLogger.PrefabNullCheckAndLog(_itemStackWidgetPrefab, nameof(_itemStackWidgetPrefab), this, GameComponent.UI))
            {
                ItemStackWidget testWidget = _itemStackWidgetPrefab.GetComponent<ItemStackWidget>();
                if (testWidget == null)
                {
                    OwlLogger.LogError($"itemWidgetPrefab {_itemStackWidgetPrefab.name} doesn't have ItemStackWidget component available!", GameComponent.UI);
                }
            }

            if (!OwlLogger.PrefabNullCheckAndLog(_filterGroup, nameof(_filterGroup), this, GameComponent.UI))
            {
                _filterGroup.SelectionChanged += OnFilterUpdated;
            }

            if (!OwlLogger.PrefabNullCheckAndLog(_closeButton, "closeButton", this, GameComponent.UI))
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        public void SetData(Inventory inventory)
        {
            if (inventory == _currentInventory)
                return;

            ClearDisplay();

            if (inventory == null)
                return;

            foreach (var kvp in inventory.ItemStacks)
            {
                OnItemStackAdded(kvp.Value);
            }

            inventory.ItemStackAdded += OnItemStackAdded;
            inventory.ItemStackUpdated += OnItemStackUpdated;
            inventory.ItemStackRemoved += OnItemStackRemoved;

            _currentInventory = inventory;
        }

        public void OnItemStackAdded(ItemStack stack)
        {
            if (!stack.ItemType.MatchesFilter(_currentInventoryFilter))
                return;

            GameObject createdObj = Instantiate(_itemStackWidgetPrefab, _itemContainer); // TODO: Pooling for ItemStackWidgets
            ItemStackWidget createdWidget = createdObj.GetComponent<ItemStackWidget>();
            createdWidget.SetData(stack, DragSource);
            _createdItemWidgets.Add(stack.ItemType.TypeId, createdWidget);
        }

        public void OnItemStackUpdated(ItemStack stack)
        {
            if (!stack.ItemType.MatchesFilter(_currentInventoryFilter))
                return;

            ItemStackWidget widget = _createdItemWidgets[stack.ItemType.TypeId];
            widget.SetData(stack, DragSource);
        }

        public void OnItemStackRemoved(long itemTypeId)
        {
            if (!_createdItemWidgets.ContainsKey(itemTypeId)) // This can happen via filtering, so we expect to see that happen a lot
            {
                OwlLogger.LogF("InventoryWindow received OnItemStackRemoved for ItemType {0} that's not displayed!", itemTypeId, GameComponent.UI, LogSeverity.Verbose);
                return;
            }

            Destroy(_createdItemWidgets[itemTypeId].gameObject);
            _createdItemWidgets.Remove(itemTypeId);
        }

        public void OnFilterUpdated(RadioButton<InventoryFilter> newButton)
        {
            // This implementation is a bit wasteful, since it fully clears & rebuilds the UI - hope it won't be too expensive
            // If this is an issue: Try pooling ItemStackWidgets first
            Inventory inventory = _currentInventory;
            ClearDisplay();
            _currentInventoryFilter = newButton.Value;
            SetData(inventory);
        }

        public void ClearDisplay()
        {
            if (_currentInventory == null)
                return;

            _currentInventory.ItemStackAdded -= OnItemStackAdded;
            _currentInventory.ItemStackUpdated -= OnItemStackUpdated;
            _currentInventory.ItemStackRemoved -= OnItemStackRemoved;

            foreach (var kvp in _createdItemWidgets)
            {
                Destroy(kvp.Value.gameObject); // TODO: Pooling for ItemStackWidgets
            }
            _createdItemWidgets.Clear();
            _currentInventory = null;
        }

        private void OnCloseButtonClicked()
        {
            gameObject.SetActive(false);
        }
    }
}

