using OwlLogging;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class OneButtonNotification : MonoBehaviour
    {
        private Action _okCallback;

        [SerializeField]
        private TMP_Text _messageText;

        [SerializeField]
        private Button _okButton;

        public void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_messageText, "messageText", this, GameComponent.UI);
            if(!OwlLogger.PrefabNullCheckAndLog(_okButton, "okButton", this, GameComponent.UI))
                _okButton.onClick.AddListener(OnOkButtonCallback);
        }

        public void SetContent(string message, Action callback)
        {
            _messageText.text = message;
            _okCallback = callback;

            gameObject.SetActive(true);
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

