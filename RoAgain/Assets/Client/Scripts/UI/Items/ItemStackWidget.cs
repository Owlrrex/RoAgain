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
        private TMP_Text _itemNameText;
        [SerializeField]
        private MouseTooltipTriggerString _tooltip;

        public ItemStack CurrentStack { get; private set; }

        [field: SerializeField]
        public ItemStackDragSource DragSource { get; private set; }

        public void Awake()
        {
            if (_typeWidget != null)
                _typeWidget.SetUseTooltip(false);
            OwlLogger.PrefabNullCheckAndLog(_tooltip, nameof(_tooltip), this, GameComponent.UI);
            LocalizedStringTable.LanguageChanged += OnLanguageChanged;
        }

        public void SetData(ItemStack stack, ItemStackDragSource sourceType)
        {
            CurrentStack = stack;
            if(_typeWidget != null)
                _typeWidget.SetData(stack.ItemType);
            _itemCountText.text = stack.ItemCount.ToString();
            // TODO: Build proper ItemType name from Modifiers
            string typeName = LocalizedStringTable.GetStringById(stack.ItemType.NameLocId);
            string fullText = typeName + " x" + stack.ItemCount.ToString();
            if(_itemNameText)
                _itemNameText.text = fullText;
            _tooltip.Message = fullText;

            DragSource = sourceType;
        }

        private void OnLanguageChanged()
        {
            if(CurrentStack != null)
                SetData(CurrentStack, DragSource);
        }

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
            if(itemStackComp._itemNameText != null)
                itemStackComp._itemNameText.enabled = false;
            itemStackComp.SetData(CurrentStack, DragSource);
            if (EmptyUiCatcher.Instance != null)
                EmptyUiCatcher.Instance.SetCatcherActive(true);
        }

        public void InitDragSelf()
        {
            if (_itemNameText != null)
                _itemNameText.enabled = false;
        }
    }
}
