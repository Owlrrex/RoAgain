using OwlLogging;
using Shared;
using TMPro;
using UnityEngine;

namespace Client
{
    // TODO: replace string-based APIs with LocStringId-based APIs & use LocalizedText components, so that the dialog will auto-localize
    public class ZeroButtonNotification : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _messageText;

        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private LocalizedStringId _defaultTitle;

        public void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_messageText, nameof(_messageText), this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_titleText, nameof(_titleText), this, GameComponent.UI);
        }

        public void SetContent(string message)
        {
            _messageText.text = message;

            gameObject.SetActive(true);
        }

        public void SetTitle(string title)
        {
            _titleText.text = title;
        }

        public void ResetStrings()
        {
            _titleText.text = _defaultTitle.Resolve();
            _messageText.text = LocalizedStringId.INVALID.Resolve();
        }

        public void Hide()
        {
            _messageText.text = "Hidden";
            gameObject.SetActive(false);
        }
    }
}

