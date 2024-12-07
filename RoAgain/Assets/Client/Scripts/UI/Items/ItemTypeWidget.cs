using OwlLogging;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class ItemTypeWidget : MonoBehaviour
    {
        [SerializeField]
        private MouseTooltipTriggerLocalized _tooltipLoc;

        [SerializeField]
        private Image _typeIcon;

        private ItemType _type;

        public void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_tooltipLoc, nameof(_tooltipLoc), this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_typeIcon, nameof(_typeIcon), this, GameComponent.UI);
        }

        public void SetData(ItemType type)
        {
            Sprite typeSprite = ItemIconTable.GetDataForId(type.VisualId)?.Sprite;
            _typeIcon.sprite = typeSprite;

            // TODO: Build proper name for ItemType from its modifiers
            _tooltipLoc.LocalizedString = type.NameLocId;
            
            _type = type;
        }

        public void SetUseTooltip(bool active)
        {
            _tooltipLoc.enabled = active;
        }
    }
}
