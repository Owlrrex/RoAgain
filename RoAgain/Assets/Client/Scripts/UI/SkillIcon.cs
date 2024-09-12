using OwlLogging;
using Shared;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Client
{
    public class SkillIcon : MonoBehaviour, IDraggableSource, IPointerClickHandler
    {
        public SkillId SkillId { get; private set; }

        public int SkillParam { get; private set; }

        public SkillSlot CurrentSlot;

        [SerializeField]
        private Image _image;

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

        public void InitDragCopy(GameObject copy)
        {
            SkillIcon dragIconComp = copy.GetComponent<SkillIcon>();
            dragIconComp.SetSkillData(SkillId, SkillParam);
        }

        public void InitDragSelf()
        {
            if (CurrentSlot != null)
            {
                CurrentSlot.OnIconRemoved();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                // TODO: Skill Tooltip showing
            }
            Clicked?.Invoke(eventData);
        }
    }
}