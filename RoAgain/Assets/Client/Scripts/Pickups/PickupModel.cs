using OwlLogging;
using Shared;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Client
{
    public class PickupModel : MonoBehaviour, IPointerClickHandler
    {
        public int PickupId => _pickup.Id;
        private PickupEntity _pickup;

        private Transform _targetTransform;

        [SerializeField]
        private GridEntityMover _mover;

        [SerializeField]
        private MouseTooltipTriggerString _tooltip;

        [SerializeField]
        private float PICKUP_ANIM_LENGTH = 0.5f;
        private float _pickupAnimProg = 0;
        private bool _pickupAnimating = false;

        [SerializeField]
        private float DROP_ANIM_LENGTH = 0.1f;
        private float _dropAnimProg = 0;
        private Vector3 _dropAnimTarget;
        private Vector3 _dropAnimStart;
        private bool _dropping = false;

        // Update is called once per frame
        void Update()
        {
            // TODO: visualize lifetime

            // TODO: Visualize Owner

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

            // TODO: Display Item Icon somewhere on the _mover.Model
        }

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