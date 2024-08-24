using OwlLogging;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shared;

namespace Client
{
    public class UIChatSystem : MonoBehaviour
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

        // This will probably be replaced by a more expansive "ChatChannelData" struct that stores more than just color
        // If functional data is also stored, part of it may be moved to Client.ChatModule
        private Dictionary<string, Color> _colorPerMessageTag = new();

        private ChatModule _chatModule;

        public void Initialize(ChatModule chatModule)
        {
            if(chatModule == null)
            {
                OwlLogger.LogError("Can't initialize UIChatSystem with null Chatmodule!", GameComponent.UI);
                return;
            }

            _chatModule = chatModule;

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

            LoadTagColors();
        }

        private void LoadTagColors()
        {
            _colorPerMessageTag.Clear();
            LoadDefaultTagColors();

            // TODO: Load any other tags this client "knows" from wherever they're stored & colors for tags
        }

        private void LoadDefaultTagColors()
        {
            _colorPerMessageTag.TryAdd(DefaultChannelTags.BROADCAST, Color.yellow);
            _colorPerMessageTag.TryAdd(DefaultChannelTags.COMMAND_ERROR, Color.red);
            _colorPerMessageTag.TryAdd(DefaultChannelTags.COMMAND_FEEDBACK, Color.green);
            _colorPerMessageTag.TryAdd(DefaultChannelTags.EMOTE, Color.grey);
            _colorPerMessageTag.TryAdd(DefaultChannelTags.GENERIC_ERROR, Color.red);
            _colorPerMessageTag.TryAdd(DefaultChannelTags.GLOBAL, Color.yellow);
            _colorPerMessageTag.TryAdd(DefaultChannelTags.GUILD, Color.blue);
            _colorPerMessageTag.TryAdd(DefaultChannelTags.PARTY, Color.cyan);
            _colorPerMessageTag.TryAdd(DefaultChannelTags.PROXIMITY, Color.white);
            _colorPerMessageTag.TryAdd(DefaultChannelTags.SKILL_ERROR, Color.red);
            _colorPerMessageTag.TryAdd(DefaultChannelTags.WHISPER, Color.magenta);
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

            comp.Initialize(message, _colorPerMessageTag);
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
            ChatMessageData data = GetChatMessage();
            if (string.IsNullOrEmpty(data.Message))
                return;

            if(string.IsNullOrEmpty(data.SenderName))
            {
                // Promote empty Whisper-box to Proximity-chat
                // TODO: Respect any ChatChannel-selecting UI
                data.ChannelTag = DefaultChannelTags.PROXIMITY;
            }
            else if(_colorPerMessageTag.ContainsKey(data.SenderName))
            {
                // Debug functionality: Allow sending to any chat-channel via the Whisper-box
                // TODO: Channel-select UI, so that name-collisions between channels & character are unambiguous
                data.ChannelTag = data.SenderName;
            }
            else
            {
                data.ChannelTag = DefaultChannelTags.WHISPER;
            }

            int sendResult = _chatModule.SendChatMessage(data);
            if(sendResult == 0)
                ChatInput.text = "";
        }

        private void Update()
        {
            if (IsChatFocused)
                return;

            // TODO: Replace with configurable hotkey
            if (Input.GetKeyDown(KeyCode.Return))
            {
                EnableChatInput();
            }
        }
    }
}

