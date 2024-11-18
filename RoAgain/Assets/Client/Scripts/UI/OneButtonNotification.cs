using OwlLogging;
using Shared;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    // TODO: replace string-based APIs with LocStringId-based APIs & use LocalizedText components, so that the dialog will auto-localize
    public class OneButtonNotification : MonoBehaviour
    {
        private Action _okCallback;

        [SerializeField]
        private TMP_Text _messageText;

        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private TMP_Text _buttonText;

        [SerializeField]
        private Button _okButton;

        [SerializeField]
        private LocalizedStringId _defaultTitle;
        [SerializeField]
        private LocalizedStringId _defaultButtonText;

        public void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_messageText, nameof(_messageText), this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_titleText, nameof(_titleText), this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_buttonText, nameof(_buttonText), this, GameComponent.UI);
            if(!OwlLogger.PrefabNullCheckAndLog(_okButton, "okButton", this, GameComponent.UI))
                _okButton.onClick.AddListener(OnOkButtonCallback);
        }

        public void SetContent(string message, Action callback)
        {
            _messageText.text = message;
            _okCallback = callback;

            gameObject.SetActive(true);
        }

        public void SetTitle(string title)
        {
            _titleText.text = title;
        }

        public void SetButtonText(string buttonText)
        {
            _buttonText.text = buttonText;
        }

        public void ResetStrings()
        {
            _titleText.text = LocalizedStringTable.GetStringById(_defaultTitle);
            _buttonText.text = LocalizedStringTable.GetStringById(_defaultButtonText);
            _messageText.text = LocalizedStringTable.GetStringById(LocalizedStringId.INVALID);
        }

        private void OnOkButtonCallback()
        {
            gameObject.SetActive(false);
            _okCallback?.Invoke();
        }

        public void Hide()
        {
            _messageText.text = "Hidden";
            gameObject.SetActive(false);
        }
    }
}

