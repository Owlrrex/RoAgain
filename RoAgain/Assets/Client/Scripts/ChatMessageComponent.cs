using OwlLogging;
using TMPro;
using UnityEngine;
using System.Collections.Generic;

namespace Client
{
    public class ChatMessageData
    {
        public int SenderId; // for showing chatbubbles
        public string Message;
        public string SenderName;
        public string ChannelTag;
    }

    public class ChatMessageComponent : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _textDisplay;

        public ChatMessageData Message { get; private set; }

        public int Initialize(ChatMessageData message, Dictionary<string, Color> colormap)
        {
            if (_textDisplay == null)
            {
                OwlLogger.LogError($"ChatMessageComponent has no TextDisplay!", GameComponent.UI);
                return -1;
            }

            Message = message;

            string fullMessage;
            if(string.IsNullOrWhiteSpace(message.SenderName))
            {
                fullMessage = message.Message;
            }
            else
            {
                fullMessage = $"{message.SenderName}: {message.Message}";
            }
            
            _textDisplay.text = fullMessage;
            SetTextColor(colormap);
            return 0;
        }

        private void SetTextColor(Dictionary<string, Color> colormap)
        {
            if (Message == null || colormap == null)
                return;

            Color color;
            if(colormap.ContainsKey(Message.ChannelTag))
            {
                color = colormap[Message.ChannelTag];
            }
            else
            {
                OwlLogger.LogError($"ChatMessage initialized with no color-mapping for ChannelTag {Message.ChannelTag}, choosing default color!", GameComponent.UI);
                color = Color.black;
            }

            _textDisplay.color = color;
        }
    }
}
