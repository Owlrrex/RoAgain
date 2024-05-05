using OwlLogging;
using Shared;
using System;
using UnityEngine;

namespace Server
{
    // This class validates against the client's internal state: Is the movement state valid? Can this action be performed? 
    // Some of the more complex queries may be moved into the respective server module's code
    // All Network-related handling should take place between CentralConnection & this class.

    public abstract class ClientConnection
    {
        public Action<ClientConnection, string, string> LoginRequestRecieved;
        public Action<ClientConnection> CharacterSelectionRequestReceived;
        public Action<ClientConnection, int> CharacterLoginReceived;
        public Action<ClientConnection, Vector2Int> MovementRequestReceived;
        public Action<ClientConnection, SkillId, int, Vector2Int> GroundSkillRequestReceived;
        public Action<ClientConnection, SkillId, int, int> EntitySkillRequestReceived;
        public Action<ClientConnection, ChatModule.ChatMessageRequestData> ChatMessageRequestReceived;
        public Action<ClientConnection, EntityPropertyType> StatIncreaseRequestReceived;
        public Action<ClientConnection, string, string> AccountCreationRequestReceived;
        public Action<ClientConnection, string, int /*TODO more params*/> CharacterCreationRequestReceived;
        public Action<ClientConnection, string> AccountDeletionRequestReceived;
        public Action<ClientConnection, int> CharacterDeletionRequestReceived;
        public Action<ClientConnection, SkillId, int> SkillPointAllocateRequestReceived;
        public Action<ClientConnection, int> ReturnAfterDeathRequestReceived;
        public Action<ClientConnection> CharacterLogoutRequestReceived;
        public Action<ClientConnection, int, bool> ConfigStorageRequestReceived;
        public Action<ClientConnection, int, bool> ConfigReadRequestReceived;

        public abstract int Initialize(CentralConnection central, int sessionId);

        public string AccountId;
        public int CharacterId = -1;
        public int EntityId = -1;

        public abstract bool IsInitialized();

        public abstract int Send(Packet packet);

        public abstract void Receive(Packet packet);

        public abstract int Shutdown();
    }

    public class ClientConnectionImpl : ClientConnection
    {
        private CentralConnection _central;

        private int _sessionId;

        public override int Initialize(CentralConnection central, int sessionId)
        {
            if(central == null)
            {
                OwlLogger.LogError("Can't initialize ClientConnection with null CentralConnection!", GameComponent.Network);
                return -1;
            }
            _central = central;
            _sessionId = sessionId;
            return 0;
        }

        public override bool IsInitialized()
        {
            return _central != null;
        }

        public override int Shutdown()
        {
            if(_central == null)
            {
                OwlLogger.LogWarning("Shutting down connection that wasn't initialized properly!", GameComponent.Network);
                return -1;
            }
            _central = null;
            _sessionId = -1;
            return 0;
        }

        // This function could be split into a single Send-function for each sending scenario
        // Assembling the packet, auto-filling available data & sending via the CentralConnection class would be handled there
        public override int Send(Packet packet)
        {
            if (!IsInitialized())
            {
                OwlLogger.LogError($"Tried to send packet {packet} when ClientConnection was uninitialized!", GameComponent.Network);
                return -1;
            }

            packet.SessionId = _sessionId;

            return _central.Send(packet);
        }

        public override void Receive(Packet packet)
        {
            OwlLogger.Log($"ServerSide ClientConnection received Packet: {packet.SerializeReflection()}", GameComponent.Network, LogSeverity.VeryVerbose);

            switch (packet)
            {
                case LoginRequestPacket loginRequestPacket:
                    LoginRequestRecieved?.Invoke(this, loginRequestPacket.Username, loginRequestPacket.Password);
                    break;
                case CharacterSelectionRequestPacket charSelRequestPacket:
                    CharacterSelectionRequestReceived?.Invoke(this);
                    break;
                case CharacterLoginPacket charLoginPacket:
                    CharacterLoginReceived?.Invoke(this, charLoginPacket.CharacterId); // have to use packet's ID here, since ClientConnection.CharacterId will only be set in response to this packet
                    break;
                case MovementRequestPacket movementPacket:
                    MovementRequestReceived?.Invoke(this, movementPacket.TargetCoordinates);
                    break;
                case SkillUseEntityRequestPacket skillPacket:
                    EntitySkillRequestReceived?.Invoke(this, skillPacket.SkillId, skillPacket.SkillLvl, skillPacket.TargetId);
                    break;
                case SkillUseGroundRequestPacket skillPacket:
                    GroundSkillRequestReceived?.Invoke(this, skillPacket.SkillId, skillPacket.SkillLvl, skillPacket.TargetCoords);
                    break;
                case ChatMessageRequestPacket chatPacket:
                    ChatModule.ChatMessageRequestData data = new()
                    {
                        Message = chatPacket.Message,
                        SenderId = chatPacket.SenderId,
                        TargetName = chatPacket.TargetName
                    };
                    ChatMessageRequestReceived?.Invoke(this, data);
                    break;
                case StatIncreaseRequestPacket statIncPacket:
                    StatIncreaseRequestReceived?.Invoke(this, statIncPacket.StatType);
                    break;
                case AccountCreationRequestPacket accCreaPacket:
                    AccountCreationRequestReceived?.Invoke(this, accCreaPacket.Username, accCreaPacket.Password);
                    break;
                case AccountDeletionRequestPacket accDelPacket:
                    AccountDeletionRequestReceived?.Invoke(this, accDelPacket.AccountId);
                    break;
                case CharacterCreationRequestPacket charCreaPacket:
                    CharacterCreationRequestReceived?.Invoke(this, charCreaPacket.Name, charCreaPacket.Gender);
                    break;
                case CharacterDeletionRequestPacket charDelPacket:
                    CharacterDeletionRequestReceived?.Invoke(this, charDelPacket.CharacterId);
                    break;
                case SkillPointAllocateRequestPacket skillPointAllocatePacket:
                    SkillPointAllocateRequestReceived?.Invoke(this, skillPointAllocatePacket.SkillId, skillPointAllocatePacket.Amount);
                    break;
                case ReturnAfterDeathRequestPacket returnToSavePacket:
                    ReturnAfterDeathRequestReceived?.Invoke(this, returnToSavePacket.CharacterId);
                    break;
                case CharacterLogoutRequestPacket charLogoutPacket:
                    CharacterLogoutRequestReceived?.Invoke(this);
                    break;
                default:
                    OwlLogger.LogError($"ServerSide ClientConnection received unsupported packet: {packet.SerializeReflection()}", GameComponent.Network);
                    break;
            }
        }
    }
}

