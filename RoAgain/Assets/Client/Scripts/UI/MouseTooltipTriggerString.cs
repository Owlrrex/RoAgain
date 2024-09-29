using OwlLogging;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Client
{
    public class MouseTooltipTriggerString : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string Message = string.Empty;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!string.IsNullOrEmpty(Message) && MouseAttachedTooltip.Instance != null)
            {
                MouseAttachedTooltip.Instance.Show(Message, this);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (MouseAttachedTooltip.Instance != null)
            {
                MouseAttachedTooltip.Instance.Hide();
            }
        }

        public void OnDisable()
        {
            if(MouseAttachedTooltip.Instance != null 
                && MouseAttachedTooltip.Instance.IsMine(this))
            {
                MouseAttachedTooltip.Instance.Hide();
            }
        }
    }
}