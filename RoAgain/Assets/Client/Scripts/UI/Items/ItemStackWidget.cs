using OwlLogging;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Client
{
    public enum ItemStackDragSource
    {
        Unknown,
        OwnInventory,
        OwnCart,
        OwnEquip,
        OwnStorage,
        TradeOwnSide,
        TradeOtherSide,
        NpcShop,
        PlayerShop,
        // Dialog displays?
    }

    public class ItemStackWidget : MonoBehaviour, IPointerClickHandler, IDraggableSource
    {
        [SerializeField]
        private ItemTypeWidget _typeWidget;
        [SerializeField]
        private TMP_Text _itemCountText;
        [SerializeField]
        private MouseTooltipTriggerString _tooltip;

        public ItemStack CurrentStack { get; private set; }

        [field: SerializeField]
        public ItemStackDragSource DragSource { get; private set; }

        public void Awake()
        {
            if (!OwlLogger.PrefabNullCheckAndLog(_typeWidget, nameof(_typeWidget), this, GameComponent.UI))
                _typeWidget.SetUseTooltip(false);
            OwlLogger.PrefabNullCheckAndLog(_tooltip, nameof(_tooltip), this, GameComponent.UI);
        }

        public void SetData(ItemStack stack, ItemStackDragSource sourceType)
        {
            CurrentStack = stack;
            _typeWidget.SetData(stack.ItemType);
            _itemCountText.text = stack.ItemCount.ToString();
            DragSource = sourceType;
            // TODO: Build proper ItemType name from Modifiers
            _tooltip.Message = LocalizedStringTable.GetStringById(stack.ItemType.NameLocId) + " x" + stack.ItemCount.ToString();
        }

        // TODO: Try moving the Drag-&-Drop-logic to its own component, which somehow calls the Init-logic

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                // TODO: Item Tooltip
            }
        }

        public void InitDragCopy(GameObject copy)
        {
            ItemStackWidget itemStackComp = copy.GetComponent<ItemStackWidget>();
            itemStackComp.SetData(CurrentStack, DragSource);
            if (EmptyUiCatcher.Instance != null)
                EmptyUiCatcher.Instance.SetCatcherActive(true);
        }

        public void InitDragSelf()
        {
            
        }
    }
}
