using OwlLogging;
using Shared;
using System;
using UnityEngine;
using static ChatModule;

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
        public Action<ClientConnection, ChatMessageRequestData> ChatMessageRequestReceived;
        public Action<ClientConnection, EntityPropertyType> StatIncreaseRequestReceived;
        public Action<ClientConnection, string, string> AccountCreationRequestReceived;
        public Action<ClientConnection, string, int /*TODO more params*/> CharacterCreationRequestReceived;
        public Action<ClientConnection, string> AccountDeletionRequestReceived;
        public Action<ClientConnection, int> CharacterDeletionRequestReceived;
        public Action<ClientConnection, SkillId, int> SkillPointAllocateRequestReceived;
        public Action<ClientConnection, int> ReturnAfterDeathRequestReceived;

        public abstract int Initialize(CentralConnection central, int sessionId);

        public string AccountId;
        public int CharacterId = -1;

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

            Type packetType = packet.GetType();
            if (packetType == typeof(LoginRequestPacket))
            {
                LoginRequestPacket loginPacket = packet as LoginRequestPacket;
                LoginRequestRecieved?.Invoke(this, loginPacket.Username, loginPacket.Password);
            }
            else if (packetType == typeof(CharacterSelectionRequestPacket))
            {
                CharacterSelectionRequestReceived?.Invoke(this);
            }
            else if (packetType == typeof(CharacterLoginPacket))
            {
                CharacterLoginPacket charLoginPacket = packet as CharacterLoginPacket;
                CharacterLoginReceived?.Invoke(this, charLoginPacket.CharacterId); // have to use packet's ID here, since ClientConnection.CharacterId will only be set in response to this packet
            }
            else if (packetType == typeof(MovementRequestPacket))
            {
                MovementRequestPacket movementPacket = packet as MovementRequestPacket;
                MovementRequestReceived?.Invoke(this, movementPacket.TargetCoordinates);
            }
            else if(packetType == typeof(SkillUseEntityRequestPacket))
            {
                SkillUseEntityRequestPacket skillPacket = packet as SkillUseEntityRequestPacket;
                EntitySkillRequestReceived?.Invoke(this, skillPacket.SkillId, skillPacket.SkillLvl, skillPacket.TargetId);
            }
            else if(packetType == typeof(SkillUseGroundRequestPacket))
            {
                SkillUseGroundRequestPacket skillPacket = packet as SkillUseGroundRequestPacket;
                GroundSkillRequestReceived?.Invoke(this, skillPacket.SkillId, skillPacket.SkillLvl, skillPacket.TargetCoords);
            }
            else if(packetType == typeof(ChatMessageRequestPacket))
            {
                ChatMessageRequestPacket chatPacket = packet as ChatMessageRequestPacket;
                ChatMessageRequestData data = new()
                {
                    Message = chatPacket.Message,
                    SenderId = chatPacket.SenderId,
                    TargetName = chatPacket.TargetName
                };
                ChatMessageRequestReceived?.Invoke(this, data);
            }
            else if(packetType == typeof(StatIncreaseRequestPacket))
            {
                StatIncreaseRequestPacket statIncPacket = packet as StatIncreaseRequestPacket;
                StatIncreaseRequestReceived?.Invoke(this, statIncPacket.StatType);
            }
            else if(packetType == typeof(AccountCreationRequestPacket))
            {
                AccountCreationRequestPacket accCreaPacket = packet as AccountCreationRequestPacket;
                AccountCreationRequestReceived?.Invoke(this, accCreaPacket.Username, accCreaPacket.Password);
            }
            else if (packetType == typeof(AccountDeletionRequestPacket))
            {
                AccountDeletionRequestPacket accDelPacket = packet as AccountDeletionRequestPacket;
                AccountDeletionRequestReceived?.Invoke(this, accDelPacket.AccountId);
            }
            else if(packetType == typeof(CharacterCreationRequestPacket))
            {
                CharacterCreationRequestPacket charCreaPacket = packet as CharacterCreationRequestPacket;
                CharacterCreationRequestReceived?.Invoke(this, charCreaPacket.Name, charCreaPacket.Gender);
            }
            else if(packetType == typeof(CharacterDeletionRequestPacket))
            {
                CharacterDeletionRequestPacket charDelPacket = packet as CharacterDeletionRequestPacket;
                CharacterDeletionRequestReceived?.Invoke(this, charDelPacket.CharacterId);
            }
            else if(packetType == typeof(SkillPointAllocateRequestPacket))
            {
                SkillPointAllocateRequestPacket skillPointAllocatePacket = packet as SkillPointAllocateRequestPacket;
                SkillPointAllocateRequestReceived?.Invoke(this, skillPointAllocatePacket.SkillId, skillPointAllocatePacket.Amount);
            }
            else if(packetType == typeof(ReturnAfterDeathRequestPacket))
            {
                ReturnAfterDeathRequestPacket returnToSavePacket = packet as ReturnAfterDeathRequestPacket;
                ReturnAfterDeathRequestReceived?.Invoke(this, returnToSavePacket.CharacterId);
            }
            else
            {
                Debug.LogError($"ServerSide ClientConnection received unsupported packet: {packet.SerializeReflection()}");
            }
        }
    }
}

