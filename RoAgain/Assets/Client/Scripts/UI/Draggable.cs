using OwlLogging;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Client
{
    public interface IDraggableSource
    {
        public void InitDragCopy(GameObject copy);

        public void InitDragSelf();
    }

    public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public bool AllowDrag;
        [field: SerializeField]
        public bool CreateNewOnDrag { get; private set; }
        [SerializeField]
        private GameObject _newDragPrefab;

        private RectTransform _dragIconTf;
        private IDraggableSource _dragSource;

        void Awake()
        {
            _dragSource = GetComponent<IDraggableSource>();
            OwlLogger.PrefabNullCheckAndLog(_dragSource, nameof(_dragSource), this, GameComponent.UI);
            if(CreateNewOnDrag)
                OwlLogger.PrefabNullCheckAndLog(_newDragPrefab, nameof(_newDragPrefab), this, GameComponent.UI);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!AllowDrag)
                return;

            RectTransform rtf = transform as RectTransform;
            Rect size = rtf.rect;
            //Transform dragParent = GetComponentInParent<Canvas>().transform;
            Transform dragParent = ClientMain.Instance.MainUiCanvas.transform;
            if (CreateNewOnDrag)
            {
                GameObject dragIcon = Instantiate(_newDragPrefab, rtf.position, rtf.rotation, dragParent);
                _dragIconTf = dragIcon.GetComponent<RectTransform>();
                _dragSource.InitDragCopy(dragIcon);
                if(dragIcon.TryGetComponent(out CanvasGroup canvasGroup))
                {
                    canvasGroup.blocksRaycasts = false;
                }
            }
            else
            {
                _dragIconTf = GetComponent<RectTransform>();
                if (TryGetComponent(out CanvasGroup canvasGroup))
                {
                    canvasGroup.blocksRaycasts = false;
                }
                _dragSource.InitDragSelf();
                rtf.SetParent(dragParent);
            }

            _dragIconTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.width);
            _dragIconTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.height);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!AllowDrag)
                return;

            _dragIconTf.anchoredPosition += eventData.delta; // If have to account for scale: delta / canvas.scaleFactor
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!AllowDrag)
                return;

            Destroy(_dragIconTf.gameObject);
        }

        public void SetCreateNewOnDrag(bool newValue)
        {
            if (newValue == CreateNewOnDrag)
                return;

            CreateNewOnDrag = newValue;

            if(CreateNewOnDrag)
                OwlLogger.PrefabNullCheckAndLog(_newDragPrefab, nameof(_newDragPrefab), this, GameComponent.UI);
        }
    }
}
