using OwlLogging;
using System.Collections.Generic;

namespace Server
{
    public class ChatModule
    {
        public class ChatMessageRequestData
        {
            public int SenderId;
            public GridEntity Sender;
            public string Message;
            public string TargetName;
        }

        private ServerMapModule _mapModule;
        private AServer _server;

        private char _serverChatCommandSymbol;
        private Dictionary<string, AChatCommand> _chatCommands = new();

        public int Initialize(ServerMapModule mapModule, AServer server)
        {
            if(mapModule == null)
            {
                OwlLogger.LogError("Can't initialize with null ServerMapModule!", GameComponent.Chat);
                return -1;
            }

            if(server == null)
            {
                OwlLogger.LogError("Can't initialize with null Server!", GameComponent.Chat);
                return -1;
            }

            SetupChatCommands();

            string configSymbol = Configuration.Instance.GetMainConfig(ConfigurationKey.ChatCommandSymbol);
            if(configSymbol == null
                || configSymbol.Length != 1)
            {
                OwlLogger.LogError($"Can't load invalid server chatcommand symbol {configSymbol}!", GameComponent.Chat);
            }
            else
            {
                _serverChatCommandSymbol = configSymbol[0];
            }

            _mapModule = mapModule;
            _server = server;
            return 0;
        }

        private void SetupChatCommands()
        {
            _chatCommands.Clear();

            _chatCommands.Add("changejob", new ChangeJobChatCommand());
            _chatCommands.Add("heal", new HealChatCommand());
            _chatCommands.Add("healid", new HealIdChatCommand());
            _chatCommands.Add("kill", new KillChatCommand());
            _chatCommands.Add("killid", new KillIdChatCommand());
            _chatCommands.Add("reloadskilldb", new ReloadSkillDbChatCommand());
        }

        public int HandleChatMessage(ChatMessageRequestData chatMessage)
        {
            // Only logged in players are allowed to send a chat-message to the server
            // TODO: This means NPCs can't send messages right now. Also, Announcements may be a problem?
            // Need an alternative flow for those, or adjust this restriction
            if (!_server.TryGetLoggedInCharacter(chatMessage.SenderId, out CharacterRuntimeData charData))
            {
                OwlLogger.LogError($"Cannot send chat message for sender id {chatMessage.SenderId} - not a logged in character!", GameComponent.Chat);
                return -1;
            }

            chatMessage.Sender = charData;
            chatMessage.Message = TrimNetworkPadding(chatMessage.Message);
            chatMessage.TargetName = TrimNetworkPadding(chatMessage.TargetName);

            if (chatMessage.Message.StartsWith(_serverChatCommandSymbol))
            {
                int commandResult =  100 * HandleServerCommand(chatMessage.Message, charData);
                if (commandResult == 0 // No error,
                    || commandResult >= 1000) // Error during command execution, but the command started execution
                    return 0;
            }

            if (!CanChat(chatMessage))
            {
                OwlLogger.Log($"Chat Message request denied: Character id {chatMessage.SenderId} can't chat.", GameComponent.Chat, LogSeverity.Verbose);
                return -2;
            }

            if (chatMessage.TargetName == ChatMessageRequestPacket.TARGET_GLOBAL)
            {
                return HandleGlobalMessage(chatMessage) * 10;
            }

            if (chatMessage.TargetName == ChatMessageRequestPacket.TARGET_PROX)
            {
                return HandleProximityMessage(chatMessage) * 10;
            }

            return HandleWhisperMessage(chatMessage) * 10;
        }

        private int HandleServerCommand(string message, CharacterRuntimeData sender)
        {
            string[] parts = message.Split(" ");
            parts[0] = parts[0].Remove(0, 1);
            if(!_chatCommands.ContainsKey(parts[0]))
            {
                OwlLogger.Log($"Received unknown server command: {message}", GameComponent.ChatCommands);
                return -1;
            }

            if (!CanChatCommandBeUsed(parts, sender))
            {
                OwlLogger.Log($"ChatCommand {parts[0]} can't be used by character {sender.Id}", GameComponent.ChatCommands);
                return -2;
            }

            // TODO: Allow more detailed feedback by the command about its error, to instruct the user
            int commandResult = 10 * _chatCommands[parts[0]].Execute(sender, parts);
            return commandResult;
        }

        // TODO: In line with the "only characters can send chat messages", this function can only process messages from players
        // Expand this to allow for NPCs, when chat-sending for NPCs is done.
        private bool CanChatCommandBeUsed(string[] parts, CharacterRuntimeData sender)
        {
            // TODO: Check permissions of the sender's account
            return true;
        }

        public bool CanChat(ChatMessageRequestData message)
        {
            if (message.Sender == null)
                return false;
            // TODO: Check mute, chat cooldown, etc stuff
            return true;
        }

        private string TrimNetworkPadding(string rawMessage)
        {
            return rawMessage.Trim('.');
        }

        private int HandleGlobalMessage(ChatMessageRequestData chatMessage)
        {
            ChatMessagePacket packet = new()
            {
                SenderId = chatMessage.SenderId,
                Message = chatMessage.Message,
                SenderName = chatMessage.Sender.Name,
                MessageScope = ChatMessagePacket.Scope.Global
            };

            foreach (CharacterRuntimeData charData in _server.LoggedInCharacters)
            {
                charData.Connection.Send(packet);
            }

            return 0;
        }

        private int HandleProximityMessage(ChatMessageRequestData chatMessage)
        {
            const int PROXIMITY_CHAT_RANGE = 20;
            ServerMapInstance map = _mapModule.GetMapInstance(chatMessage.Sender.MapId);
            if (map == null)
            {
                OwlLogger.LogError($"Map {chatMessage.Sender.MapId} not available for Proximity chat!", GameComponent.Chat);
                return -1;
            }

            ChatMessagePacket packet = new()
            {
                SenderId = chatMessage.SenderId,
                Message = chatMessage.Message,
                SenderName = chatMessage.Sender.Name,
                MessageScope = ChatMessagePacket.Scope.Proximity
            };

            foreach (CharacterRuntimeData charData in map.Grid.GetOccupantsInRangeSquareLowAlloc<CharacterRuntimeData>(chatMessage.Sender.Coordinates, PROXIMITY_CHAT_RANGE))
            {
                charData.Connection.Send(packet);
            }

            return 0;
        }

        private int HandleWhisperMessage(ChatMessageRequestData chatMessage)
        {
            // TODO: Allow whispers only to players for now. if we ever have whispers to NPCs, an additional system is needed here - we can't iterate over all Entities on all maps
            ChatMessagePacket packet = new()
            {
                SenderId = chatMessage.SenderId,
                Message = chatMessage.Message,
                SenderName = chatMessage.Sender.Name,
                MessageScope = ChatMessagePacket.Scope.Whisper
            };

            foreach (CharacterRuntimeData charData in _server.LoggedInCharacters)
            {
                if (charData.Name == chatMessage.TargetName)
                {
                    charData.Connection.Send(packet);
                    return SendSenderFeedback(chatMessage);
                }
            }

            OwlLogger.Log($"Tried to send Whisper to character {chatMessage.TargetName} that wasn't found!", GameComponent.Chat, LogSeverity.Verbose);
            return -1;
        }

        private int SendSenderFeedback(ChatMessageRequestData chatMessage)
        {
            if (chatMessage.Sender is not CharacterRuntimeData charSender)
                return 0;

            ChatMessagePacket feedbackMessagePacket = new()
            {
                SenderId = chatMessage.SenderId,
                Message = chatMessage.Message,
                SenderName = $"To {chatMessage.TargetName}",
                MessageScope = ChatMessagePacket.Scope.Whisper
            };

            charSender.Connection.Send(feedbackMessagePacket);
            return 0;
        }
    }
}