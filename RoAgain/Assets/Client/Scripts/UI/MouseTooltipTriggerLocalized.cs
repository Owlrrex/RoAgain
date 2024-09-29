using OwlLogging;
using Shared;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Client
{
    public class MouseTooltipTriggerLocalized : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public LocalizedStringId LocalizedStringId = LocalizedStringId.INVALID;

        void Awake()
        {
            if (LocalizedStringId == LocalizedStringId.INVALID)
            {
                OwlLogger.LogError("MouseTooltipTrigger doesn't have LocalizedStringId set!", GameComponent.UI);
            }
        }

        public void OnDisable()
        {
            if(MouseAttachedTooltip.Instance != null
                && MouseAttachedTooltip.Instance.IsMine(this))
            MouseAttachedTooltip.Instance.Hide();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if(MouseAttachedTooltip.Instance != null)
            {
                MouseAttachedTooltip.Instance.Show(LocalizedStringId, this);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if(MouseAttachedTooltip.Instance != null)
            {
                MouseAttachedTooltip.Instance.Hide();
            }
        }
    }
}