using OwlLogging;
using TMPro;
using UnityEngine;

namespace Client
{
    public class ChatMessageData
    {
        public int SenderId; // for showing chatbubbles
        public string Message;
        public string SenderName;
        public ChatMessagePacket.Scope Scope;
    }

    public class ChatMessageComponent : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _textDisplay;

        public ChatMessageData Message { get; private set; }

        public int Initialize(ChatMessageData message)
        {
            if (_textDisplay == null)
            {
                OwlLogger.LogError($"ChatMessageComponent has no TextDisplay!", GameComponent.UI);
                return -1;
            }

            Message = message;

            string fullMessage = $"{message.SenderName}: {message.Message}";
            _textDisplay.text = fullMessage;
            SetTextColor();
            return 0;
        }

        private void SetTextColor()
        {
            if (Message == null)
                return;

            _textDisplay.color = Message.Scope switch
            {
                ChatMessagePacket.Scope.Global => Color.yellow,
                ChatMessagePacket.Scope.Whisper => Color.magenta,
                ChatMessagePacket.Scope.Proximity => Color.white,
                _ => Color.grey,
            };
        }
    }
}
