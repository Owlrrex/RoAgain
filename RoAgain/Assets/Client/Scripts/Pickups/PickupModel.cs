using OwlLogging;
using Shared;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Client
{
    public abstract class PickupModel : MonoBehaviour, IPointerClickHandler
    {
        public int PickupId => _pickup.Id;
        protected PickupEntity _pickup;

        private Transform _targetTransform;

        [SerializeField]
        protected GridEntityMover _mover;

        [SerializeField]
        protected MouseTooltipTriggerString _tooltip;

        [SerializeField]
        protected float PICKUP_ANIM_LENGTH = 0.5f;
        private float _pickupAnimProg = 0;
        private bool _pickupAnimating = false;

        [SerializeField]
        protected float DROP_ANIM_LENGTH = 0.1f;
        private float _dropAnimProg = 0;
        private Vector3 _dropAnimTarget;
        private Vector3 _dropAnimStart;
        private bool _dropping = false;

        [SerializeField]
        protected float LIFETIME_HINT_BASE_FREQUENCY = 5.0f;
        protected float LIFETIME_HINT_REFERENCE_LIFETIME = 30.0f;
        private float _lifetimeHighlightTimer;

        private bool _lastCanPickup = false;

        // Update is called once per frame
        protected void Update()
        {
            _pickup.LifeTime.Update(Time.deltaTime); // Not the best spot to do it, but it doesn't matter if there's no model anyway
            _lifetimeHighlightTimer += Time.deltaTime;
            float lifetimeFrequencyTargetInterval = 0.2f + LIFETIME_HINT_BASE_FREQUENCY * _pickup.LifeTime.RemainingValue / LIFETIME_HINT_REFERENCE_LIFETIME;
            if(_lifetimeHighlightTimer >= lifetimeFrequencyTargetInterval)
            {
                PlayLifetimeHint();
                _lifetimeHighlightTimer -= lifetimeFrequencyTargetInterval;
            }

            bool userCanPickup = CanLocalPlayerPickup();
            if (_lastCanPickup != userCanPickup)
            {
                _lastCanPickup = userCanPickup;
                SetCanPickupHighlight(userCanPickup);
            }

            if(_pickupAnimating)
            {
                if(_pickupAnimProg < 1.0f)
                    _pickupAnimProg += Time.deltaTime / PICKUP_ANIM_LENGTH;
                Vector3 newPos = Vector3.Lerp(_mover.Model.transform.position, _targetTransform.position, _pickupAnimProg);
                _mover.Model.transform.position = newPos;
            }
            else if(_dropping)
            {
                if (_dropAnimProg < 1.0f)
                    _dropAnimProg = Mathf.Clamp01(_dropAnimProg + Time.deltaTime / DROP_ANIM_LENGTH);
                    
                Vector3 newPos = Vector3.Lerp(_dropAnimStart, _dropAnimTarget, _dropAnimProg);
                _mover.Model.transform.localPosition = newPos;
                if (_dropAnimProg == 1)
                    _dropping = false;
            }
        }

        public void Initialize(PickupEntity pickup)
        {
            OwlLogger.PrefabNullCheckAndLog(_mover, nameof(_mover), this, GameComponent.Items);
            OwlLogger.PrefabNullCheckAndLog(_tooltip, nameof(_tooltip), this, GameComponent.Items);

            _pickup = pickup;

            Vector3 modelPos = _mover.Model.transform.localPosition;
            modelPos.x = Random.value - 0.5f;
            modelPos.z = Random.value - 0.5f;
            _mover.Model.transform.localPosition = modelPos;

            ItemType type = ClientMain.Instance.InventoryModule.GetKnownItemType(pickup.ItemTypeId);
            if(type == null)
            {
                OwlLogger.LogError($"Tried to initialize Pickup for ItemType {pickup.ItemTypeId} that's not known!", GameComponent.Items);
                return;
            }
            _tooltip.Message = $"{pickup.Count}x {LocalizedStringTable.GetStringById(type.NameLocId)}";

            SetItemTypeDisplay();

            bool userCanPickup = CanLocalPlayerPickup();
            _lastCanPickup = userCanPickup;
            SetCanPickupHighlight(userCanPickup);
        }

        private bool CanLocalPlayerPickup()
        {
            return _pickup.OwnerEntityId <= 0
                || (ClientMain.Instance.CurrentCharacterData != null
                    && _pickup.OwnerEntityId == ClientMain.Instance.CurrentCharacterData.Id);
        }

        protected abstract void SetItemTypeDisplay();

        protected abstract void PlayLifetimeHint();

        protected abstract void SetCanPickupHighlight(bool canPickup);

        public void StartDropAnimation()
        {
            _dropAnimTarget = _mover.Model.transform.localPosition;
            _dropAnimStart = _dropAnimTarget + new Vector3(0, 1, 0);
            _dropping = true;
        }

        public void StartPickupAnimation(Transform targetTransform)
        {
            _targetTransform = targetTransform;
            _tooltip.enabled = false;
            if(_dropping)
            {
                _dropping = false;
                // Is this necessary/ugly?
                // _mover.Model.transform.localPosition = _dropAnimTarget;
            }
            _pickupAnimating = true;
        }

        public bool IsPickupAnimationFinished()
        {
            return _pickupAnimProg >= 1.0f;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (_pickupAnimating)
                return;

            ClientMain.Instance.ConnectionToServer.Send(new PickupRequestPacket() { PickupId = _pickup.Id });
        }
    }
}