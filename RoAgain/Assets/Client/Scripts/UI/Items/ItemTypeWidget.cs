using OwlLogging;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class ItemTypeWidget : MonoBehaviour
    {
        [SerializeField]
        private MouseTooltipTriggerString _tooltip;

        [SerializeField]
        private Image _typeIcon;

        private bool _useTooltip = true;

        public void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_tooltip, nameof(_tooltip), this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_typeIcon, nameof(_typeIcon), this, GameComponent.UI);
        }

        public void SetData(ItemType type)
        {
            Sprite typeSprite = ItemIconTable.GetDataForId(type.VisualId)?.Sprite;
            _typeIcon.sprite = typeSprite;

            if(_useTooltip)
            {
                // TODO: Build proper name for ItemType from its modifiers
                _tooltip.Message = LocalizedStringTable.GetStringById(type.NameLocId);
            }
        }

        public void SetUseTooltip(bool active)
        {
            _useTooltip = active;
        }
    }
}
