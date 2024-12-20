using OwlLogging;
using Shared;
using SuperSimpleTcp;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    public abstract class ServerConnection
    {
        public Action<int> SessionReceived;
        public Action<AccountLoginResponse> AccountLoginResponseReceived;
        public Action<List<CharacterSelectionData>> CharacterSelectionDataReceived;
        public Action<int> CharacterLoginResponseReceived;
        public Action<UnitMovementInfo> UnitMovementReceived;
        public Action<GridEntityData> GridEntityDataReceived;
        public Action<BattleEntityData> BattleEntityDataReceived;
        public Action<RemoteCharacterData> RemoteCharacterDataReceived;
        public Action<LocalCharacterData> LocalCharacterDataReceived;
        public Action<int> EntityRemovedReceived;
        public Action DisconnectDetected;
        //public Action<string, Vector2Int> MapChangeReceived;
        public Action<CellEffectData> CellEffectGroupPlacedReceived;
        public Action<int> CellEffectGroupRemovedReceived;
        public Action<int, int, bool, bool, int> DamageTakenReceived;
        public Action<int, SkillId, TimerFloat, int, Coordinate> CastProgressReceived;
        public Action<int, SkillId, int, float> EntitySkillExecutionReceived;
        public Action<int, SkillId, Coordinate, float> GroundSkillExecutionReceived;
        public Action<SkillId, int> LocalPlayerEntitySkillQueuedReceived;
        public Action<SkillId, Coordinate> LocalPlayerGroundSkillQueuedReceived;
        public Action<ChatMessageData> ChatMessageReceived;
        public Action<int, float> HpChangeReceived;
        public Action<int, float> SpChangeReceived;
        public Action<int, SkillId, SkillFailReason> SkillFailReceived;
        public Action<EntityPropertyType, Stat> StatUpdateReceived;
        public Action<EntityPropertyType, int> StatCostUpdateReceived;
        public Action<int> StatPointUpdateReceived;
        public Action<int, int> ExpUpdateReceived;
        public Action<int, int, bool, int> LevelUpdateReceived;
        public Action<int> AccountCreationResponseReceived;
        public Action<int> CharacterCreationResponseReceived;
        public Action<SkillTreeEntry> SkillTreeEntryUpdateReceived;
        public Action<SkillId> SkillTreeEntryRemoveReceived;
        public Action<int> SkillPointAllocateResponseReceived;
        public Action<ConfigKey, bool, int, bool> ConfigValueReceived;
        public Action<int, int> InventoryReceived;
        public Action<int, long, int> ItemStackReceived;
        public Action<ItemType> ItemTypeReceived;
        public Action<EquippableItemType> EquippableItemTypeReceived;
        public Action<ConsumableItemType> ConsumableItemTypeReceived;
        public Action<int, long> ItemStackRemovedReceived;
        public Action<int, int> WeightChangedReceived;
        public Action<PickupData> PickupDataReceived;
        public Action<int, int> PickupRemovedReceived;
        public Action<int, EquipmentSlot, long> EquipmentSlotReceived;

        public abstract int Initialize(string serverConfigDataHere);

        public abstract void Update();

        public abstract int Shutdown();

        public abstract int Send(Packet packet);

        public abstract void Receive(Packet packet);

        public abstract void Disconnect();

        public abstract void ResetCharacterSelectionData();

        public bool IsAlive { get; protected set; }
    }

    public class ServerConnectionImpl : ServerConnection
    {
        private string _serverNetworkId = "";
        private List<CharacterSelectionData> _characterSelectionBuffer;

        private int _sessionId = -1;

        SimpleTcpClient _client;
        private byte[] _remainingData = new byte[0];
        private System.Collections.Concurrent.ConcurrentQueue<string> _readyPacketStrings = new();
        private List<byte> _dataBuffer = new();
        private object _dataLock = new();

        public override int Initialize(string serverConfigDataHere)
        {
            string serverNetworkId = serverConfigDataHere;
            
            _serverNetworkId = serverNetworkId;

            try
            {
                _client = new(_serverNetworkId);
                _client.Settings.StreamBufferSize = Packet.DATA_BUFFER_SIZE;

                _client.Events.Connected += OnConnected;
                _client.Events.Disconnected += OnDisconnected;
                _client.Events.DataReceived += OnDataReceived;
                _client.Events.DataSent += OnDataSent;

                _client.Connect();
            }
            catch (Exception e)
            {
                OwlLogger.LogError(e.ToString(), GameComponent.Network);
                return -1;
            }

            IsAlive = true;
            return 0;
        }

        private void OnConnected(object sender, ConnectionEventArgs args)
        {
            OwlLogger.LogF("Client connected to server at {0} ({1})", args.IpPort, args.Reason, GameComponent.Network, LogSeverity.Verbose);
        }

        private void OnDisconnected(object sender, ConnectionEventArgs args)
        {
            OwlLogger.LogF("Client disconnected from server at {0} ({1})", args.IpPort, args.Reason, GameComponent.Network, LogSeverity.Verbose);

            // This function may be called on a separate thread, so we use IsAlive as optional communications
            IsAlive = false;
            DisconnectDetected?.Invoke();
        }

        private void OnDataReceived(object sender, DataReceivedEventArgs args)
        {
            if(OwlLogger.CurrentLogVerbosity >= LogSeverity.VeryVerbose) // filter log early to avoid encoding-call
            {
                OwlLogger.LogF("Client received Data from {0}: {1}", args.IpPort, System.Text.Encoding.UTF8.GetString(args.Data.Array, 0, args.Data.Count), GameComponent.Network, LogSeverity.VeryVerbose);
            }

            string[] completedPacketStrings;
            lock(_dataLock)
            {
                _dataBuffer.AddRange(_remainingData);
                _dataBuffer.AddRange(args.Data);
                completedPacketStrings = Packet.SplitIntoPackets(_dataBuffer.ToArray(), out _remainingData);
                _dataBuffer.Clear();
            }

            foreach (string completedPacketStr in completedPacketStrings)
            {
                _readyPacketStrings.Enqueue(completedPacketStr);
            }
        }

        private void OnDataSent(object sender, DataSentEventArgs args)
        {
            OwlLogger.Log($"Client sent {args.BytesSent} Bytes to {args.IpPort}", GameComponent.Network, LogSeverity.VeryVerbose);
        }

        public override void Update()
        {
            while (_readyPacketStrings.TryDequeue(out string packetString))
            {
                Packet packet = Packet.DeserializeJson(packetString);
                Receive(packet);
            }
        }

        public override int Shutdown()
        {
            if(_client != null)
            {
                // TODO: Send a logout packet as well?
                _client.Disconnect();
                _client.Dispose();
                _client = null;
            }

            _serverNetworkId = "";
            _characterSelectionBuffer = null;
            _sessionId = -1;
            _remainingData = new byte[0];
            _readyPacketStrings.Clear();
            _dataBuffer.Clear();
            return 0;
        }

        public override int Send(Packet packet)
        {
            if (_client == null)
            {
                OwlLogger.LogError("Tried to send Packet on non-initialized Connection!", GameComponent.Network);
                return -1;
            }
            packet.SessionId = _sessionId;

            byte[] serializedB = packet.SerializeJson();
            if( serializedB == null )
            {
                return -1;
            }

            try
            {
                _client.Send(serializedB);
            }
            catch(InvalidOperationException e)
            {
                OwlLogger.LogError($"Exception while sending: {e.Message}", GameComponent.Network);
                return -2;
            }

            return 0;
        }

        public override void Receive(Packet packet)
        {
            if(packet == null)
            {
                OwlLogger.LogError("Client received null packet!", GameComponent.Network);
                return;
            }

            OwlLogger.LogF("Client received packet type {0}", packet.PacketType, GameComponent.Network, LogSeverity.VeryVerbose);

            switch(packet)
            {
                case SessionCreationPacket sessionPacket:
                    _sessionId = sessionPacket.AssignedSessionId;
                    OwlLogger.Log($"Client got assigned SessionId {_sessionId}", GameComponent.Network);
                    SessionReceived?.Invoke(_sessionId);
                    break;
                case AccountLoginResponsePacket loginPacket:
                    ReceiveAccountLoginResponse(loginPacket);
                    break;
                case CharacterSelectionDataPacket charSelPacket:
                    ReceiveCharacterSelectionData(charSelPacket);
                    break;
                case CharacterLoginResponsePacket charLoginPacket:
                    ReceiveCharacterLoginResponse(charLoginPacket);
                    break;
                case EntityPathUpdatePacket pathPacket:
                    ReceiveMovement(pathPacket);
                    break;
                case GridEntityDataPacket gridDataPacket:
                    ReceiveGridEntityData(gridDataPacket);
                    break;
                case BattleEntityDataPacket battleDataPacket:
                    ReceiveBattleEntityData(battleDataPacket);
                    break;
                case RemoteCharacterDataPacket remoteDataPacket:
                    ReceiveRemoteCharacterData(remoteDataPacket);
                    break;
                case LocalCharacterDataPacket localDataPacket:
                    ReceiveLocalCharacterData(localDataPacket);
                    break;
                case EntityRemovedPacket entityRemovedPacket:
                    ReceiveEntityRemoved(entityRemovedPacket);
                    break;
                case CellEffectGroupPlacedPacket groupPlacedPacket:
                    ReceiveCellEffectGroupPlaced(groupPlacedPacket);
                    break;
                case CellEffectGroupRemovedPacket groupRemovedPacket:
                    ReceiveCellEffectGroupRemoved(groupRemovedPacket);
                    break;
                case DamageTakenPacket damagePacket:
                    ReceiveDamageTakenPacket(damagePacket);
                    break;
                case CastProgressPacket castPacket:
                    ReceiveCastProgressPacket(castPacket);
                    break;
                case EntitySkillExecutePacket entitySkillPacket:
                    ReceiveEntitySkillPacket(entitySkillPacket);
                    break;
                case GroundSkillExecutePacket groundSkillPacket:
                    ReceiveGroundSkillPacket(groundSkillPacket);
                    break;
                case ChatMessagePacket chatMsgPacket:
                    ReceiveChatMessagePacket(chatMsgPacket);
                    break;
                case LocalizedChatMessagePacket localizedChatMessagePacket:
                    ReceiveLocalizedChatMessagePacket(localizedChatMessagePacket);
                    break;
                case HpUpdatePacket hpUpdatePacket:
                    ReceiveHpUpdatePacket(hpUpdatePacket);
                    break;
                case SpUpdatePacket spUpdatePacket:
                    ReceiveSpUpdatePacket(spUpdatePacket);
                    break;
                case SkillFailPacket skillFailPacket:
                    ReceiveSkillFailPacket(skillFailPacket);
                    break;
                case StatUpdatePacket statUpdatePacket:
                    ReceiveStatUpdatePacket(statUpdatePacket);
                    break;
                case StatCostUpdatePacket statCostUpdatePacket:
                    ReceiveStatCostUpdatePacket(statCostUpdatePacket);
                    break;
                case StatPointUpdatePacket statPointPacket:
                    ReceiveStatPointUpdatePacket(statPointPacket);
                    break;
                case ExpUpdatePacket expPacket:
                    ReceiveExpUpdatePacket(expPacket);
                    break;
                case LevelUpPacket levelPacket:
                    ReceiveLevelUpPacket(levelPacket);
                    break;
                case AccountCreationResponsePacket accCreaRespPacket:
                    AccountCreationResponseReceived?.Invoke(accCreaRespPacket.Result);
                    break;
                case CharacterCreationResponsePacket charCreaRespPacket:
                    CharacterCreationResponseReceived?.Invoke(charCreaRespPacket.Result);
                    break;
                case LocalPlayerEntitySkillQueuedPacket entitySkillQueuedPacket:
                    LocalPlayerEntitySkillQueuedReceived?.Invoke(entitySkillQueuedPacket.SkillId, entitySkillQueuedPacket.TargetId);
                    break;
                case LocalPlayerGroundSkillQueuedPacket groundSkillQueuedPacket:
                    LocalPlayerGroundSkillQueuedReceived?.Invoke(groundSkillQueuedPacket.SkillId, groundSkillQueuedPacket.Target);
                    break;
                case SkillTreeEntryPacket skillTreeUpdatePacket:
                    SkillTreeEntry entry = new(skillTreeUpdatePacket);
                    SkillTreeEntryUpdateReceived?.Invoke(entry);
                    break;
                case SkillTreeRemovePacket skillTreeRemovePacket:
                    SkillTreeEntryRemoveReceived?.Invoke(skillTreeRemovePacket.SkillId);
                    break;
                case SkillPointUpdatePacket skillPointResponsePacket:
                    SkillPointAllocateResponseReceived?.Invoke(skillPointResponsePacket.RemainingSkillPoints);
                    break;
                case ConfigValuePacket configValuePacket:
                    ConfigValueReceived?.Invoke((ConfigKey)configValuePacket.Key, configValuePacket.Exists, configValuePacket.Value, configValuePacket.IsAccountStorage);
                    break;
                case InventoryPacket invPacket:
                    InventoryReceived?.Invoke(invPacket.InventoryId, invPacket.OwnerId);
                    break;
                case ItemStackPacket itemStackPacket:
                    ItemStackReceived?.Invoke(itemStackPacket.InventoryId, itemStackPacket.ItemTypeId, itemStackPacket.ItemCount);
                    break;
                case EquippableItemTypePacket equipItemTypePacket:
                    EquippableItemTypeReceived?.Invoke(EquippableItemType.FromPacket(equipItemTypePacket));
                    break;
                case ConsumableItemTypePacket consumableItemTypePacket:
                    ConsumableItemTypeReceived?.Invoke(ConsumableItemType.FromPacket(consumableItemTypePacket));
                    break;
                case ItemTypePacket itemTypePacket:
                    ItemTypeReceived?.Invoke(ItemType.FromPacket(itemTypePacket));
                    break;                
                case ItemStackRemovedPacket itemStackRemovedPacket:
                    ItemStackRemovedReceived?.Invoke(itemStackRemovedPacket.InventoryId, itemStackRemovedPacket.ItemTypeId);
                    break;
                case WeightPacket weightPacket:
                    WeightChangedReceived?.Invoke(weightPacket.EntityId, weightPacket.NewCurrentWeight);
                    break;
                case PickupDataPacket pickupDataPacket:
                    PickupDataReceived?.Invoke(PickupData.FromPacket(pickupDataPacket));
                    break;
                case PickupRemovedPacket pickupRemovedPacket:
                    PickupRemovedReceived?.Invoke(pickupRemovedPacket.PickupId, pickupRemovedPacket.PickedUpEntityId);
                    break;
                case EquipmentSlotPacket equipSlotPacket:
                    EquipmentSlotReceived?.Invoke(equipSlotPacket.OwnerEntityId, equipSlotPacket.Slot, equipSlotPacket.ItemTypeId);
                    break;
                default:
                    OwlLogger.LogError($"Clientside DummyServerConnection received unsupported packet: {packet.SerializeReflection()}", GameComponent.Network);
                    break;
            }
        }

        private void ReceiveAccountLoginResponse(AccountLoginResponsePacket packet)
        {
            if (packet.IsSuccessful)
                OwlLogger.Log("Login completed successfully", GameComponent.Other);
            else
                OwlLogger.Log("Login failed", GameComponent.Other);

            // TODO: If successful, store sessionId for validation of future packets?

            AccountLoginResponse response = new() { IsSuccessful = packet.IsSuccessful, SessionId = packet.SessionId };
            AccountLoginResponseReceived?.Invoke(response);
        }

        private void ReceiveCharacterSelectionData(CharacterSelectionDataPacket packet)
        {
            _characterSelectionBuffer ??= new List<CharacterSelectionData>(new CharacterSelectionData[packet.Count]);

            // Verify buffer & packet seem to match
            if (packet.Count != _characterSelectionBuffer.Capacity)
            {
                OwlLogger.LogError($"Character Selection Buffer mismatch, packet = {packet.SerializeReflection()}", GameComponent.Network);
                return;
            }

            if (packet.Count == 0 && packet.Index == 0)
            {
                CharacterSelectionDataReceived?.Invoke(_characterSelectionBuffer);
                return;
            }

            _characterSelectionBuffer[packet.Index] = packet.Data;
            if (_characterSelectionBuffer.Count == packet.Count
                && _characterSelectionBuffer.TrueForAll(x => x != null))
            {
                CharacterSelectionDataReceived?.Invoke(_characterSelectionBuffer);
            }
        }

        private void ReceiveCharacterLoginResponse(CharacterLoginResponsePacket packet)
        {
            CharacterLoginResponseReceived?.Invoke(packet.Result);
        }

        public override void ResetCharacterSelectionData()
        {
            _characterSelectionBuffer = null;
        }

        //private void ReceiveCharacterRuntimeData(RemoteCharacterDataPacket packet)
        //{
        //    OwlLogger.Log($"Received CharacterRuntimeData for Id {packet.UnitId}", GameComponent.Network);
        //    CharacterRuntimeClientData data = new(packet);
        //    CharacterSharedRuntimeDataReceived?.Invoke(data);
        //}

        //private void ReceiveCharacterLoginCompleted(CharacterLoginCompletedPacket packet)
        //{
        //    bool successful = packet.CharacterId > 0;

        //    OwlLogger.Log($"Received CharacterLoginCompleted for Id {packet.CharacterId}", GameComponent.Network);
        //    CharacterLoginCompletedReceived?.Invoke(successful);
        //}

        private void ReceiveMovement(EntityPathUpdatePacket packet)
        {
            // Translate into custom structure to isolate from packet format
            UnitMovementInfo moveInfo = new(packet);

            UnitMovementReceived?.Invoke(moveInfo);
        }

        private void ReceiveGridEntityData(GridEntityDataPacket packet)
        {
            if (packet.Path.AllCells.Count == 0 && packet.Path.Corners.Count == 0)
            {
                // keeping this if around also blocks the elseif to trigger for empty paths
                // This is how Json serializes a null path - we compensate
                //packet.Path = null;
            }
            else if (packet.Path.AllCells.Count < 2 || packet.Path.Corners.Count < 2)
            {
                // these aren't sensible paths and shouldn't have been sent
                OwlLogger.LogError($"Non-Null path that's below sensible size received!", GameComponent.Network);
                packet.Path = null;
            }

            GridEntityDataReceived?.Invoke(GridEntityData.FromPacket(packet));
        }

        private void ReceiveBattleEntityData(BattleEntityDataPacket packet)
        {
            if (packet.Path.AllCells.Count == 0 && packet.Path.Corners.Count == 0)
            {
                // keeping this if around also blocks the elseif to trigger for empty paths
                // This is how Json serializes a null path - we compensate
                //packet.Path = null;
            }
            else if (packet.Path.AllCells.Count < 2 || packet.Path.Corners.Count < 2)
            {
                // these aren't sensible paths and shouldn't have been sent
                OwlLogger.LogError($"Non-Null path that's below sensible size received!", GameComponent.Network);
                packet.Path = null;
            }

            BattleEntityDataReceived?.Invoke(BattleEntityData.FromPacket(packet));
        }

        private void ReceiveRemoteCharacterData(RemoteCharacterDataPacket packet)
        {
            if (packet.Path.AllCells.Count == 0 && packet.Path.Corners.Count == 0)
            {
                // keeping this if around also blocks the elseif to trigger for empty paths
                // This is how Json serializes a null path - we compensate
                //packet.Path = null;
            }
            else if (packet.Path.AllCells.Count < 2 || packet.Path.Corners.Count < 2)
            {
                // these aren't sensible paths and shouldn't have been sent
                OwlLogger.LogError($"Non-Null path that's below sensible size received!", GameComponent.Network);
                packet.Path = null;
            }

            RemoteCharacterDataReceived?.Invoke(RemoteCharacterData.FromPacket(packet));
        }

        private void ReceiveLocalCharacterData(LocalCharacterDataPacket packet)
        {
            if (packet.Path.AllCells.Count == 0 && packet.Path.Corners.Count == 0)
            {
                // keeping this if around also blocks the elseif to trigger for empty paths
                // This is how Json serializes a null path - we compensate
                //packet.Path = null;
            }
            else if (packet.Path.AllCells.Count < 2 || packet.Path.Corners.Count < 2)
            {
                // these aren't sensible paths and shouldn't have been sent
                OwlLogger.LogError($"Non-Null path that's below sensible size received!", GameComponent.Network);
                packet.Path = null;
            }

            LocalCharacterDataReceived?.Invoke(LocalCharacterData.FromPacket(packet));
        }

        private void ReceiveEntityRemoved(EntityRemovedPacket packet)
        {
            EntityRemovedReceived?.Invoke(packet.EntityId);
        }

        private void ReceiveCellEffectGroupPlaced(CellEffectGroupPlacedPacket packet)
        {
            CellEffectData data = CellEffectData.FromPacket(packet);

            CellEffectGroupPlacedReceived?.Invoke(data);
        }

        private void ReceiveCellEffectGroupRemoved(CellEffectGroupRemovedPacket packet)
        {
            CellEffectGroupRemovedReceived?.Invoke(packet.GroupId);
        }

        //private void ReceiveMapChange(MapChangePacket packet)
        //{
        //    MapChangeReceived?.Invoke(packet.NewMapId, packet.NewMapCoordinates);
        //}

        private void ReceiveDamageTakenPacket(DamageTakenPacket packet)
        {
            DamageTakenReceived?.Invoke(packet.EntityId, packet.Damage, packet.IsSpDamage, packet.IsCrit, packet.ChainCount);
        }

        private void ReceiveCastProgressPacket(CastProgressPacket packet)
        {
            TimerFloat castTime = new()
            {
                MaxValue = packet.CastTimeTotal,
                RemainingValue = packet.CastTimeRemaining
            };
            CastProgressReceived?.Invoke(packet.CasterId, packet.SkillId, castTime, packet.TargetId, packet.TargetCoords);
        }

        private void ReceiveEntitySkillPacket(EntitySkillExecutePacket packet)
        {
            EntitySkillExecutionReceived?.Invoke(packet.UserId, packet.SkillId, packet.TargetId, packet.AnimCd);
        }

        private void ReceiveGroundSkillPacket(GroundSkillExecutePacket packet)
        {
            GroundSkillExecutionReceived?.Invoke(packet.UserId, packet.SkillId, packet.TargetCoords, packet.AnimCd);
        }

        private void ReceiveChatMessagePacket(ChatMessagePacket packet)
        {
            ChatMessageData data = new()
            {
                SenderId = packet.SenderId,
                Message = packet.Message,
                SenderName = packet.SenderName,
                ChannelTag = packet.ChannelTag
            };
            ChatMessageReceived?.Invoke(data);
        }

        private void ReceiveLocalizedChatMessagePacket(LocalizedChatMessagePacket packet)
        {
            ChatMessageData data = new()
            {
                SenderId = packet.SenderId,
                Message = packet.MessageLocId.Resolve(),
                SenderName = packet.SenderName,
                ChannelTag = packet.ChannelTag
            };
            ChatMessageReceived?.Invoke(data);
        }

        private void ReceiveHpUpdatePacket(HpUpdatePacket packet)
        {
            HpChangeReceived?.Invoke(packet.EntityId, packet.NewHp);
        }

        private void ReceiveSpUpdatePacket(SpUpdatePacket packet)
        {
            SpChangeReceived?.Invoke(packet.EntityId, packet.NewSp);
        }

        private void ReceiveSkillFailPacket(SkillFailPacket packet)
        {
            SkillFailReceived?.Invoke(packet.EntityId, packet.SkillId, packet.Reason);
        }

        private void ReceiveStatUpdatePacket(StatUpdatePacket packet)
        {
            StatUpdateReceived?.Invoke(packet.Type, packet.NewValue);
        }

        private void ReceiveStatCostUpdatePacket(StatCostUpdatePacket packet)
        {
            StatCostUpdateReceived?.Invoke(packet.Type, packet.NewCost);
        }

        private void ReceiveStatPointUpdatePacket(StatPointUpdatePacket packet)
        {
            StatPointUpdateReceived?.Invoke(packet.NewRemaining);
        }

        private void ReceiveExpUpdatePacket(ExpUpdatePacket packet)
        {
            ExpUpdateReceived?.Invoke(packet.CurrentBaseExp, packet.CurrentJobExp);
        }

        private void ReceiveLevelUpPacket(LevelUpPacket packet)
        {
            LevelUpdateReceived?.Invoke(packet.EntityId, packet.Level, packet.IsJob, packet.RequiredExp);
        }

        public override void Disconnect()
        {
            if (_client == null)
                return;

            _client.Disconnect();
        }
    }

    // -----------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------

    public class DummyServerConnection : ServerConnection
    {
        private int _serverNetworkId = 0;
        private List<CharacterSelectionData> _characterSelectionBuffer;

        private int _sessionId;

        public override int Initialize(string serverConfigDataHere)
        {
            // for now: Server initializes internet too
            

            int serverNetworkId = ADummyInternet.Instance.ConnectToServer(this);
            if(serverNetworkId < 0)
            {
                // Error Handling
            }
            _serverNetworkId = serverNetworkId;
            return 0;
        }


        public override int Shutdown()
        {
            if(_serverNetworkId > 0)
            {
                ADummyInternet.Instance.DisconnectFromServer(this);
            }

            return 0;
        }

        public override int Send(Packet packet)
        {
            packet.SessionId = _sessionId;
            ADummyInternet.Instance.SendPacket(this, _serverNetworkId, packet);
            return 0;
        }

        public override void Receive(Packet packet)
        {
            if (packet is AccountLoginResponsePacket loginResponsePacket)
            {
                _sessionId = loginResponsePacket.SessionId;
                ReceiveLoginResponse(packet as AccountLoginResponsePacket);
            }
            else if (packet is CharacterSelectionDataPacket charSelectPacket)
            {
                ReceiveCharacterSelectionData(charSelectPacket);
            }
            else if (packet is RemoteCharacterDataPacket charDataPacket)
            {
                ReceiveCharacterRuntimeData(charDataPacket);
            }
            //else if (packet is CharacterLoginCompletedPacket loginCompletedPacket)
            //{
            //    ReceiveCharacterLoginCompleted(loginCompletedPacket);
            //}
            else if (packet is EntityPathUpdatePacket pathUpdatePacket)
            {
                ReceiveMovement(pathUpdatePacket);
            }
            else if(packet is GridEntityDataPacket entityDataPacket)
            {
                ReceiveEntityData(entityDataPacket);
            }
            else if(packet is CellEffectGroupPlacedPacket effectPlacedPacket)
            {
                ReceiveCellEffectGroupPlaced(effectPlacedPacket);
            }
            else if(packet is CellEffectGroupRemovedPacket effectRemovedPacket)
            {
                ReceiveCellEffectGroupRemoved(effectRemovedPacket);
            }
            else
            {
                Debug.LogError($"Clientside DummyServerConnection received unsupported packet: {packet.SerializeReflection()}");
            }
        }

        private void ReceiveLoginResponse(AccountLoginResponsePacket packet)
        {
            if (packet.IsSuccessful)
                OwlLogger.Log("Login completed successfully", GameComponent.Other);
            else
                OwlLogger.Log("Login failed", GameComponent.Other);

            AccountLoginResponse response = new() { IsSuccessful = packet.IsSuccessful, SessionId = packet.SessionId };
            AccountLoginResponseReceived?.Invoke(response);
        }

        private void ReceiveCharacterSelectionData(CharacterSelectionDataPacket packet)
        {
            if (_characterSelectionBuffer == null)
            {
                _characterSelectionBuffer = new List<CharacterSelectionData>(packet.Count);
            }
            else
            {
                // Verify buffer & packet seem to match
                if (packet.Count != _characterSelectionBuffer.Capacity)
                {
                    OwlLogger.LogError($"Character Selection Buffer mismatch, packet = {packet.SerializeReflection()}", GameComponent.Network);
                    return;
                }
            }

            _characterSelectionBuffer.Insert(packet.Index, packet.Data);
            if (_characterSelectionBuffer.Count == packet.Count
                && _characterSelectionBuffer.TrueForAll(x => x != null))
            {
                CharacterSelectionDataReceived?.Invoke(_characterSelectionBuffer);
            }
        }

        private void ReceiveCharacterRuntimeData(RemoteCharacterDataPacket packet)
        {
            OwlLogger.Log($"Received CharacterRuntimeData for Id {packet.EntityId}", GameComponent.Network);
            //ACharacterRuntimeData data = new(packet);
            //CharacterSharedRuntimeDataReceived?.Invoke(data);
        }

        //private void ReceiveCharacterLoginCompleted(CharacterLoginCompletedPacket packet)
        //{
        //    bool successful = packet.CharacterId > 0;

        //    OwlLogger.Log($"Received CharacterLoginCompleted for Id {packet.CharacterId}", GameComponent.Network);
        //    CharacterLoginCompletedReceived?.Invoke(successful);
        //}

        private void ReceiveMovement(EntityPathUpdatePacket packet)
        {
            if(packet == null)
            {
                OwlLogger.LogError("Client received null movement packet!", GameComponent.Network);
                return;
            }

            // Translate into custom structure to isolate from packet format
            UnitMovementInfo moveInfo = new(packet);

            UnitMovementReceived?.Invoke(moveInfo);
        }

        private void ReceiveEntityData(GridEntityDataPacket packet)
        {
            if(packet == null)
            {
                OwlLogger.LogError("Client received null EntityDataPacket!", GameComponent.Network);
                return;
            }

            GridEntityDataReceived?.Invoke(GridEntityData.FromPacket(packet));
        }

        public void ReceiveCellEffectGroupPlaced(CellEffectGroupPlacedPacket packet)
        {
            if (packet == null)
            {
                OwlLogger.LogError("Client received null CellEffectGroupPlacedPacket!", GameComponent.Network);
                return;
            }

            CellEffectData data = CellEffectData.FromPacket(packet);

            CellEffectGroupPlacedReceived?.Invoke(data);
        }

        public void ReceiveCellEffectGroupRemoved(CellEffectGroupRemovedPacket packet)
        {
            if (packet == null)
            {
                OwlLogger.LogError("Client received null CellEffectGroupRemovedPacket!", GameComponent.Network);
                return;
            }

            CellEffectGroupRemovedReceived?.Invoke(packet.GroupId);
        }

        public override void Update()
        {
            
        }

        public override void Disconnect()
        {
            OwlLogger.Log("DummyClient Disconnect called - does nothing.", GameComponent.Other);
        }

        public override void ResetCharacterSelectionData()
        {
            throw new NotImplementedException();
        }
    }
}

