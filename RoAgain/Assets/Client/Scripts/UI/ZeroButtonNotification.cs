using OwlLogging;
using TMPro;
using UnityEngine;

namespace Client
{
    public class ZeroButtonNotification : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _messageText;

        public void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_messageText, "messageText", this, GameComponent.UI);
        }

        public void SetContent(string message)
        {
            _messageText.text = message;

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _messageText.text = "Hidden";
            gameObject.SetActive(false);
        }
    }
}

