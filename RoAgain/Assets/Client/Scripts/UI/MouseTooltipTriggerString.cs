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
                MouseAttachedTooltip.Instance.Show(Message);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (MouseAttachedTooltip.Instance != null)
            {
                MouseAttachedTooltip.Instance.Hide();
            }
        }
    }
}