using OwlLogging;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class PickupModel2D : PickupModel
    {
        [SerializeField]
        private Color _ownedPickupColor;
        [SerializeField]
        private Color _foreignPickupColor;
        [SerializeField]
        private Color _lifetimeHighlightColor;

        [SerializeField]
        protected float LIFETIME_HINT_LENGTH = 0.1f;

        private Image _image;

        private TimerFloat _lifetimeHintTimer = new();
        private Color _baseColor;

        private new void Update()
        {
            base.Update();
            _lifetimeHintTimer.Update(Time.deltaTime);
            if(_lifetimeHintTimer.IsFinished())
                _image.color = _baseColor;
        }

        protected override void PlayLifetimeHint()
        {
            _image.color = _lifetimeHighlightColor;
            _lifetimeHintTimer.Initialize(LIFETIME_HINT_LENGTH);
        }

        protected override void SetCanPickupHighlight(bool canPickup)
        {
            _image ??= _mover.Model.GetComponentInChildren<Image>();
            if (_image == null)
            {
                OwlLogger.LogError("Can't find Image on PickupModel!", GameComponent.UI);
                return;
            }

            _baseColor = canPickup ? _ownedPickupColor : _foreignPickupColor;
            _image.color = _baseColor;
        }

        protected override void SetItemTypeDisplay()
        {
            _image ??= _mover.Model.GetComponentInChildren<Image>();
            if (_image == null)
            {
                OwlLogger.LogError("Can't find Image on PickupModel!", GameComponent.UI);
                return;
            }

            ItemType type = ClientMain.Instance.InventoryModule.GetKnownItemType(_pickup.ItemTypeId);
            if (type == null)
            {
                OwlLogger.LogError($"Tried to display PickupModel for ItemType {_pickup.ItemTypeId} that's not known!", GameComponent.UI);
                return;
            }
            
            ItemIconData iconData = ItemIconTable.GetDataForId(type.VisualId);
            if(iconData == null)
            {
                OwlLogger.LogError($"Tried to display PickupModel for ItemType {_pickup.ItemTypeId} that's not in ItemIconTable!", GameComponent.UI);
                return;
            }

            _image.sprite = iconData.Sprite;
        }
    }
}

