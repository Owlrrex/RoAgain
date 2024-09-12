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
        public bool CopyOnDrag;
        [SerializeField]
        private CanvasGroup _canvasGroup;

        private RectTransform _dragIconTf;
        private IDraggableSource _dragSource;

        void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_canvasGroup, nameof(_canvasGroup), this, GameComponent.UI);
            _dragSource = GetComponent<IDraggableSource>();
            OwlLogger.PrefabNullCheckAndLog(_dragSource, nameof(_dragSource), this, GameComponent.UI);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!AllowDrag)
                return;

            RectTransform rtf = transform as RectTransform;
            Rect size = rtf.rect;
            Transform dragParent = GetComponentInParent<Canvas>().transform;
            if (CopyOnDrag)
            {
                GameObject dragIcon = Instantiate(gameObject, rtf.position, rtf.rotation, dragParent);
                _dragIconTf = dragIcon.GetComponent<RectTransform>();
                _dragSource.InitDragCopy(dragIcon);
                dragIcon.GetComponent<Draggable>()._canvasGroup.blocksRaycasts = false;
            }
            else
            {
                _dragIconTf = GetComponent<RectTransform>();
                _canvasGroup.blocksRaycasts = false;
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
    }
}
