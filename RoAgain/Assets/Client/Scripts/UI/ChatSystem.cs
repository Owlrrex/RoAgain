using OwlLogging;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Client
{
    public class ChatSystem : MonoBehaviour
    {
        public const int CHAT_MESSAGE_MAX_COUNT = 100;
        public TMP_InputField ChatInput;
        public TMP_InputField ChatTargetInput;
        public ScrollRect ChatMessageScroll;
        public Button ChatSendButton;

        [SerializeField]
        private GameObject ChatMessagePrefab;
        private List<ChatMessageComponent> _createdMessages = new();

        private float _lastScrollValue = 0;
        private bool _hasAddedMessageThisFrame = false;

        public bool IsChatFocused => ChatInput.isFocused || ChatTargetInput.isFocused;

        public void Initialize()
        {
            if(!OwlLogger.PrefabNullCheckAndLog(ChatInput, "ChatInput", this, GameComponent.UI))
                ChatInput.onSubmit.AddListener(OnChatSubmit);
            if (!OwlLogger.PrefabNullCheckAndLog(ChatTargetInput, "ChatTargetInput", this, GameComponent.UI))
                ChatTargetInput.onSubmit.AddListener(OnChatSubmit);
            if(!OwlLogger.PrefabNullCheckAndLog(ChatSendButton, "ChatSendButton", this, GameComponent.UI))
                ChatSendButton.onClick.AddListener(OnChatSendClicked);
            OwlLogger.PrefabNullCheckAndLog(ChatMessagePrefab, "ChatMessagePrefab", this, GameComponent.UI);

            if(!OwlLogger.PrefabNullCheckAndLog(ChatMessageScroll, "ChatMessageScroll", this, GameComponent.UI))
            {
                ChatMessageScroll.onValueChanged.AddListener(OnScrollValueChanged);
                ChatMessageScroll.verticalNormalizedPosition = 0;
            }
        }

        public void EnableChatInput()
        {
            ChatInput.Select();
        }

        public int DisplayInChatWindow(ChatMessageData message)
        {
            GameObject newMessage = Instantiate(ChatMessagePrefab);
            if (newMessage == null)
            {
                OwlLogger.LogError($"Can't instantiate ChatMessagePrefab!", GameComponent.UI);
                return -1;
            }

            ChatMessageComponent comp = newMessage.GetComponent<ChatMessageComponent>();
            if (comp == null)
            {
                OwlLogger.LogError($"ChatMessagePrefab has no ChatMessageComponent!", GameComponent.UI);
                return -2;
            }

            comp.Initialize(message);
            // Calculate the correct size for the chatmessage after its text has been set.
            LayoutRebuilder.ForceRebuildLayoutImmediate(comp.GetComponent<RectTransform>());
            newMessage.transform.SetParent(ChatMessageScroll.content, false);

            _createdMessages.Add(comp);

            while (_createdMessages.Count > CHAT_MESSAGE_MAX_COUNT)
            {
                Destroy(_createdMessages[0].gameObject);
                _createdMessages.RemoveAt(0);
            }

            _hasAddedMessageThisFrame = true;
            return 0;
        }

        private void OnScrollValueChanged(Vector2 newNormalizedValue)
        {
            float newNormalizedVerticalValue = newNormalizedValue.y;
            float precision = 0.00005f;
            if (_hasAddedMessageThisFrame)
            {
                if (_lastScrollValue < precision)
                {
                    // Stick to bottom if message was added & we were at bottom
                    ChatMessageScroll.verticalScrollbar.value = 0;
                    _lastScrollValue = 0;
                    _hasAddedMessageThisFrame = false;
                    return;
                }
            }

            _lastScrollValue = newNormalizedVerticalValue;
            _hasAddedMessageThisFrame = false;
        }

        public ChatMessageData GetChatMessage()
        {
            ChatMessageData data = new()
            {
                Message = ChatInput.text,
                SenderName = ChatTargetInput.text
            };
            return data;
        }

        private void OnChatSubmit(string msg)
        {
            OnChatSendClicked();
        }

        private void OnChatSendClicked()
        {
            // TODO: Separate Chat system that sends the packet, instad of general UI class
            ChatMessageData data = GetChatMessage();
            if (string.IsNullOrEmpty(data.Message))
                return;

            string targetName = string.IsNullOrEmpty(data.SenderName) ? ChatMessageRequestPacket.TARGET_PROX : data.SenderName;
            // TODO: Set Target with more distinction: Prox, Global, Whisper
            // Prox should be default for empty targetname, Global should be by user's choice (like #map), Whisper otherwise

            if (targetName.Length > ChatMessageRequestPacket.NAME_LENGTH)
            {
                OwlLogger.LogError($"Can't send chatmessage to targetName of length {targetName.Length}: Too long!", GameComponent.UI);
                return;
            }
            else if (targetName.Length < ChatMessageRequestPacket.NAME_LENGTH)
            {
                targetName = targetName.PadRight(ChatMessageRequestPacket.NAME_LENGTH, '.');
            }

            if (data.Message.Length > ChatMessageRequestPacket.MESSAGE_LENGTH)
            {
                OwlLogger.LogError($"Can't send chatmessage content of length {data.Message.Length}: Too long!", GameComponent.UI);
                return;
            }
            else if (data.Message.Length < ChatMessageRequestPacket.MESSAGE_LENGTH)
            {
                data.Message = data.Message.PadRight(ChatMessageRequestPacket.MESSAGE_LENGTH, '.');
            }

            ChatMessageRequestPacket packet = new()
            {
                SenderId = ClientMain.Instance.CurrentCharacterData.Id,
                Message = data.Message,
                TargetName = targetName
            };
            ClientMain.Instance.ConnectionToServer.Send(packet);
            ChatInput.text = "";
        }

        private void Update()
        {
            if (IsChatFocused)
                return;

            if (Input.GetKeyDown(KeyCode.Return))
            {
                EnableChatInput();
            }
        }
    }
}

