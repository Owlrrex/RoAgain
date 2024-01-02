using OwlLogging;
using Shared;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Client
{
    public class SkillIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public SkillId SkillId { get; private set; }

        public int SkillParam { get; private set; }

        public SkillSlot CurrentSlot;

        [SerializeField]
        private Image _image;
        [SerializeField]
        private CanvasGroup _canvasGroup;

        private RectTransform _dragIconTf;

        public bool CopyOnDrag = false;
        public bool AllowDrag = true;

        public Action<PointerEventData> Clicked;


        public void SetSkillData(SkillId skillId, int param)
        {
            if (skillId == SkillId.Unknown)
            {
                OwlLogger.LogError($"Can't display skillId {skillId} in SkillSlot!", GameComponent.UI);
                return;
            }

            SkillId = skillId;
            SkillParam = param;
            Sprite spriteForSkill = SkillClientDataTable.GetDataForId(SkillId)?.Sprite;
            _image.sprite = spriteForSkill;
            // If skillParam (skill level / item count) display is moved to this component: Update here
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
                GameObject dragIcon = Instantiate(PlayerUI.Instance.SkillIconPrefab, rtf.position, rtf.rotation, dragParent);
                _dragIconTf = dragIcon.GetComponent<RectTransform>();
                SkillIcon dragIconComp = dragIcon.GetComponent<SkillIcon>();
                dragIconComp.SetSkillData(SkillId, SkillParam);
                dragIconComp._canvasGroup.blocksRaycasts = false;
            }
            else
            {
                _dragIconTf = GetComponent<RectTransform>();
                _canvasGroup.blocksRaycasts = false;
                if (CurrentSlot != null)
                {
                    CurrentSlot.OnIconRemoved();
                }
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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                // TODO: Skill Tooltip showing
            }
            Clicked?.Invoke(eventData);
        }

        private void PlanSkillPoints(SkillTreeEntry skillEntry, int planAmount)
        {

        }
    }
}