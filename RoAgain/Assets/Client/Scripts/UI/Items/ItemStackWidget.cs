using OwlLogging;
using Shared;
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

    [ExecuteAlways]
    public class ItemStackWidget : MonoBehaviour, IPointerClickHandler, IDraggableSource
    {
        [SerializeField]
        private bool _showIcon;
        [SerializeField]
        private bool _showName;
        [SerializeField]
        private bool _showCountInText;
        [SerializeField]
        private bool _showCountInIcon;
        [SerializeField]
        private ItemTypeWidget _typeWidget;
        [SerializeField]
        private TMP_Text _itemCountText;
        [SerializeField]
        private TMP_Text _itemNameText; // For Layouting
        [SerializeField]
        private LocalizedStringText _itemNameLocText; // For setting string on _itemNameText
        [SerializeField]
        private MouseTooltipTriggerLocalized _tooltipLoc;
        [SerializeField]
        private bool _iconToTheRight;

        public ItemType CurrentType { get; private set; }
        public int CurrentCount { get; private set; }

        [field: SerializeField]
        public ItemStackDragSource DragSource { get; private set; }

        private float _lastHeight;
        private float _lastWidth;

        public void Awake()
        {
            if (!Application.isPlaying)
                return;

            if(_showName)
            {
                OwlLogger.PrefabNullCheckAndLog(_itemNameLocText, nameof(_itemNameLocText), this, GameComponent.UI);
                OwlLogger.PrefabNullCheckAndLog(_itemNameText, nameof(_itemNameText), this, GameComponent.UI);
            }
                

            if(_showIcon)
            {
                if (!OwlLogger.PrefabNullCheckAndLog(_typeWidget, nameof(_typeWidget), this, GameComponent.UI))
                    _typeWidget.SetUseTooltip(false);
            }

            if(!OwlLogger.PrefabNullCheckAndLog(_tooltipLoc, nameof(_tooltipLoc), this, GameComponent.UI))
            {
                _tooltipLoc.LocalizedString = new CompositeLocalizedString() { FormatString = new LocalizedStringId(-1) };
            }
        }

        private void Update()
        {
            RectTransform thisRt = transform as RectTransform;
            Rect thisRect = thisRt.rect;
            if (thisRect.width == _lastWidth && thisRect.height == _lastHeight)
                return;

            // size the icon to aspect-ratio 1:1
            float newIconWidth = 0;
            if (_showIcon)
            {
                RectTransform rt = _typeWidget.transform as RectTransform;
                newIconWidth = thisRect.height;
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newIconWidth);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newIconWidth);

                Vector2 localPos = rt.anchoredPosition;
                if (_iconToTheRight)
                    localPos.x = thisRect.width - newIconWidth;
                else
                    localPos.x = 0;
                rt.anchoredPosition = localPos;

                // TODO: Adjust Icon-count position
            }

            // offset the text to the right by icon's width, assuming it's anchored at the same horizontal position as the icon
            if (_showName)
            {
                RectTransform rt = _itemNameText.rectTransform;
                Vector3 localPos = rt.anchoredPosition;
                if (_iconToTheRight)
                    localPos.x = 0;
                else
                    localPos.x = newIconWidth;
                rt.anchoredPosition = localPos;

                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, thisRect.width - newIconWidth);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, thisRect.height);
            }

            _lastWidth = thisRt.rect.width;
            _lastHeight = thisRt.rect.height;
        }

        public void SetData(ItemType type, int count, ItemStackDragSource sourceType)
        {
            CurrentType = type;
            CurrentCount = count;
            DragSource = sourceType;

            if (_showIcon)
                _typeWidget.SetData(CurrentType);
            _typeWidget.enabled = _showIcon;

            if(count > 1)
            {
                if (_showCountInIcon)
                    _itemCountText.text = CurrentCount.ToString();
                _itemCountText.enabled = _showCountInIcon;
            }
            else
            {
                _itemCountText.enabled = false;
            }

            // TODO: Build proper ItemType name from Modifiers
            ILocalizedString locText;
            if (CurrentCount > 1 && _showCountInText)
            {
                locText = new CompositeLocalizedString()
                {
                    FormatString = new LocalizedStringId(246),
                    Arguments = { CurrentType.NameLocId, CurrentCount }
                };
            }
            else
            {
                locText = CurrentType.NameLocId;
            }

            if (_showName)
            {
                _itemNameLocText.SetLocalizedString(locText);
            }
            _tooltipLoc.LocalizedString = locText;
        }

        public void SetData(ItemStack stack, ItemStackDragSource sourceType)
        {
            SetData(stack.ItemType, stack.ItemCount, sourceType);
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
            itemStackComp._showName = false;
            itemStackComp._showIcon = true;
            itemStackComp._showCountInIcon = false;

            itemStackComp.SetData(CurrentType, CurrentCount, DragSource);

            if (EmptyUiCatcher.Instance != null)
                EmptyUiCatcher.Instance.SetCatcherActive(true);
        }

        public void InitDragSelf()
        {
            _showIcon = true;
            _showName = false;
            SetData(CurrentType, CurrentCount, DragSource);
        }
    }
}
