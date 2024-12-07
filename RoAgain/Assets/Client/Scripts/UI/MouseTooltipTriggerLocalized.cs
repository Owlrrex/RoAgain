using OwlLogging;
using Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Client
{
    public class MouseTooltipTriggerLocalized : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, FormerlySerializedAs("LocalizedStringId")]
        private LocalizedStringId _defaultLocalizedStringId = LocalizedStringId.INVALID;

        public ILocalizedString LocalizedString;

        void Awake()
        {
            if (!ILocalizedString.IsValid(_defaultLocalizedStringId))
            {
                OwlLogger.LogWarning("MouseTooltipTrigger doesn't have LocalizedStringId set!", GameComponent.UI);
            }
            LocalizedString = _defaultLocalizedStringId;
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
                MouseAttachedTooltip.Instance.Show(LocalizedString, this);
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