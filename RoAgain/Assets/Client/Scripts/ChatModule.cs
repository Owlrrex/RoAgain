using OwlLogging;
using Shared;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    // TODO: Use this as the functional ChatSystem, and leave only UI-related stuff in ChatSystem
    public class ChatModule
    {
        private MapModule _mapModule;

        public int Initialize(MapModule mapModule)
        {
            if(mapModule == null)
            {
                OwlLogger.LogError("Can't initialize ChatModule with null MapModule!", GameComponent.Chat);
                return -1;
            }

            _mapModule = mapModule;

            return 0;
        }

        public void Shutdown()
        {

        }

        public void OnChatMessageReceived(ChatMessageData data)
        {
            DisplayChatMessageIfVisible(data);
            PlayerUI.Instance.ChatSystem.DisplayInChatWindow(data);
        }

        public void DisplayChatMessageIfVisible(ChatMessageData data)
        {
            // No overhead display wanted for whispers
            if (data.ChannelTag == DefaultChannelTags.WHISPER)
                return;

            if (data.SenderId <= 0)
                return;

            string print;
            if (string.IsNullOrWhiteSpace(data.SenderName))
            {
                print = data.Message;
            }
            else
            {
                print = $"{data.SenderName}: {data.Message}";
            }

            // Player model is easily available
            if (data.SenderId == ClientMain.Instance.CurrentCharacterData.Id)
            {
                PlayerMain.Instance.SetSkilltext(print, 5);
                return;
            }

            // TODO: Allow non-BattleEntities to display Chatmessages, probably by having a dedicated chat-display instead of reusing SetSkilltext()
            BattleEntityModelMain bModel = _mapModule.GetComponentFromEntityDisplay<BattleEntityModelMain>(data.SenderId);
            if (bModel == null)
                return;

            bModel.SetSkilltext(print, 5);
        }

        public int SendChatMessage(ChatMessageData data)
        {
            if (data == null)
            {
                OwlLogger.LogError("Can't send null chatmessage!", GameComponent.Chat);
                return -1;
            }

            if (data.SenderName.Length > ChatMessageRequestPacket.NAME_LENGTH)
            {
                OwlLogger.LogError($"Can't send chatmessage to targetName of length {data.SenderName.Length}: Too long!", GameComponent.UI);
                return -2;
            }
            else if (data.SenderName.Length < ChatMessageRequestPacket.NAME_LENGTH)
            {
                data.SenderName = data.SenderName.PadRight(ChatMessageRequestPacket.NAME_LENGTH, '.');
            }

            if (data.Message.Length > ChatMessageRequestPacket.MESSAGE_LENGTH)
            {
                OwlLogger.LogError($"Can't send chatmessage content of length {data.Message.Length}: Too long!", GameComponent.UI);
                return -3;
            }
            else if (data.Message.Length < ChatMessageRequestPacket.MESSAGE_LENGTH)
            {
                data.Message = data.Message.PadRight(ChatMessageRequestPacket.MESSAGE_LENGTH, '.');
            }

            ChatMessageRequestPacket packet = new()
            {
                SenderId = ClientMain.Instance.CurrentCharacterData.Id,
                Message = data.Message,
                TargetName = data.SenderName,
                ChannelTag = data.ChannelTag,
            };
            return ClientMain.Instance.ConnectionToServer.Send(packet);
        }
    }
}
