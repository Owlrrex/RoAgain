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
        public Action<int, SkillId, TimerFloat, int, Vector2Int> CastProgressReceived;
        public Action<int, SkillId, int, float> EntitySkillExecutionReceived;
        public Action<int, SkillId, Vector2Int, float> GroundSkillExecutionReceived;
        public Action<SkillId, int> LocalPlayerEntitySkillQueuedReceived;
        public Action<SkillId, Vector2Int> LocalPlayerGroundSkillQueuedReceived;
        public Action<ChatMessageData> ChatMessageReceived;
        public Action<int, int> HpChangeReceived;
        public Action<int, int> SpChangeReceived;
        public Action<int, SkillId, SkillFailReason> SkillFailReceived;
        public Action<EntityPropertyType, Stat> StatUpdateReceived;
        public Action<EntityPropertyType, StatFloat> StatFloatUpdateReceived;
        public Action<EntityPropertyType, int> StatCostUpdateReceived;
        public Action<int> StatPointUpdateReceived;
        public Action<int, int> ExpUpdateReceived;
        public Action<int, int, bool, int> LevelUpdateReceived;
        public Action<int> AccountCreationResponseReceived;
        public Action<int> CharacterCreationResponseReceived;
        public Action<SkillTreeEntry> SkillTreeEntryUpdateReceived;
        public Action<SkillId> SkillTreeEntryRemoveReceived;
        public Action<int> SkillPointAllocateResponseReceived;

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
        private System.Collections.Concurrent.ConcurrentBag<Packet> _readyPackets = new();

        public override int Initialize(string serverConfigDataHere)
        {
            string serverNetworkId = serverConfigDataHere;
            
            _serverNetworkId = serverNetworkId;

            try
            {
                _client = new(_serverNetworkId);
                _client.Settings.StreamBufferSize = 8192;

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

            List<byte> allData = new(_remainingData);
            allData.AddRange(args.Data);
            string[] completedPackets = Packet.SplitIntoPackets(allData.ToArray(), out _remainingData);
            foreach (string completedPacketStr in completedPackets)
            {
                Packet packet = Packet.DeserializeJson(completedPacketStr);
                _readyPackets.Add(packet);
            }
        }

        private void OnDataSent(object sender, DataSentEventArgs args)
        {
            OwlLogger.Log($"Client sent {args.BytesSent} Bytes to {args.IpPort}", GameComponent.Network, LogSeverity.VeryVerbose);
        }

        public override void Update()
        {
            bool canTake = _readyPackets.TryTake(out Packet packet);
            while (canTake)
            {
                Receive(packet);
                if (_readyPackets.Count == 0)
                    break;
                canTake = _readyPackets.TryTake(out packet);
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
            _readyPackets.Clear();
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

            _client.Send(serializedB);

            return 0;
        }

        public override void Receive(Packet packet)
        {
            if(packet == null)
            {
                OwlLogger.LogError("Client received null packet!", GameComponent.Network);
                return;
            }

            if (packet is SessionCreationPacket sessionPacket)
            {
                _sessionId = sessionPacket.AssignedSessionId;
                OwlLogger.Log($"Client got assigned SessionId {_sessionId}", GameComponent.Network);
                SessionReceived?.Invoke(_sessionId);
            }
            else if (packet is AccountLoginResponsePacket loginPacket)
            {
                ReceiveAccountLoginResponse(loginPacket);
            }
            else if (packet is CharacterSelectionDataPacket charSelPacket)
            {
                ReceiveCharacterSelectionData(charSelPacket);
            }
            else if (packet is CharacterLoginResponsePacket charLoginPacket)
            {
                ReceiveCharacterLoginResponse(charLoginPacket);
            }
            else if (packet is EntityPathUpdatePacket pathPacket)
            {
                ReceiveMovement(pathPacket);
            }
            else if (packet is GridEntityDataPacket gridDataPacket)
            {
                ReceiveGridEntityData(gridDataPacket);
            }
            else if (packet is BattleEntityDataPacket battleDataPacket)
            {
                ReceiveBattleEntityData(battleDataPacket);
            }
            else if (packet is RemoteCharacterDataPacket remoteDataPacket)
            {
                ReceiveRemoteCharacterData(remoteDataPacket);
            }
            else if (packet is LocalCharacterDataPacket localDataPacket)
            {
                ReceiveLocalCharacterData(localDataPacket);
            }
            else if (packet is EntityRemovedPacket entityRemovedPacket)
            {
                ReceiveEntityRemoved(entityRemovedPacket);
            }
            //else if(packetType == typeof(MapChangePacket))
            //{
            //    ReceiveMapChange(packet as MapChangePacket);
            //}
            else if (packet is CellEffectGroupPlacedPacket groupPlacedPacket)
            {
                ReceiveCellEffectGroupPlaced(groupPlacedPacket);
            }
            else if (packet is CellEffectGroupRemovedPacket groupRemovedPacket)
            {
                ReceiveCellEffectGroupRemoved(groupRemovedPacket);
            }
            else if (packet is DamageTakenPacket damagePacket)
            {
                ReceiveDamageTakenPacket(damagePacket);
            }
            else if (packet is CastProgressPacket castPacket)
            {
                ReceiveCastProgressPacket(castPacket);
            }
            else if (packet is EntitySkillExecutePacket entitySkillPacket)
            {
                ReceiveEntitySkillPacket(entitySkillPacket);
            }
            else if (packet is GroundSkillExecutePacket groundSkillPacket)
            {
                ReceiveGroundSkillPacket(groundSkillPacket);
            }
            else if (packet is ChatMessagePacket chatMsgPacket)
            {
                ReceiveChatMessagePacket(chatMsgPacket);
            }
            else if (packet is HpUpdatePacket hpUpdatePacket)
            {
                ReceiveHpUpdatePacket(hpUpdatePacket);
            }
            else if (packet is SpUpdatePacket spUpdatePacket)
            {
                ReceiveSpUpdatePacket(spUpdatePacket);
            }
            else if (packet is SkillFailPacket skillFailPacket)
            {
                ReceiveSkillFailPacket(skillFailPacket);
            }
            else if (packet is StatUpdatePacket statUpdatePacket)
            {
                ReceiveStatUpdatePacket(statUpdatePacket);
            }
            else if (packet is StatFloatUpdatePacket statFloatUpdatePacket)
            {
                ReceiveStatFloatUpdatePacket(statFloatUpdatePacket);
            }
            else if (packet is StatCostUpdatePacket statCostUpdatePacket)
            {
                ReceiveStatCostUpdatePacket(statCostUpdatePacket);
            }
            else if (packet is StatPointUpdatePacket statPointPacket)
            {
                ReceiveStatPointUpdatePacket(statPointPacket);
            }
            else if (packet is ExpUpdatePacket expPacket)
            {
                ReceiveExpUpdatePacket(expPacket);
            }
            else if (packet is LevelUpPacket levelPacket)
            {
                ReceiveLevelUpPacket(levelPacket);
            }
            else if (packet is AccountCreationResponsePacket accCreaRespPacket)
            {
                AccountCreationResponseReceived?.Invoke(accCreaRespPacket.Result);
            }
            else if(packet is CharacterCreationResponsePacket charCreaRespPacket)
            {
                CharacterCreationResponseReceived?.Invoke(charCreaRespPacket.Result);
            }
            else if(packet is LocalPlayerEntitySkillQueuedPacket entitySkillQueuedPacket)
            {
                LocalPlayerEntitySkillQueuedReceived?.Invoke(entitySkillQueuedPacket.SkillId, entitySkillQueuedPacket.TargetId);
            }
            else if(packet is LocalPlayerGroundSkillQueuedPacket groundSkillQueuedPacket)
            {
                LocalPlayerGroundSkillQueuedReceived?.Invoke(groundSkillQueuedPacket.SkillId, groundSkillQueuedPacket.Target);
            }
            else if(packet is SkillTreeEntryPacket skillTreeUpdatePacket)
            {
                SkillTreeEntry entry = new(skillTreeUpdatePacket);
                SkillTreeEntryUpdateReceived?.Invoke(entry);
            }
            else if(packet is SkillTreeRemovePacket skillTreeRemovePacket)
            {
                SkillTreeEntryRemoveReceived?.Invoke(skillTreeRemovePacket.SkillId);
            }
            else if(packet is SkillPointUpdatePacket skillPointResponsePacket)
            {
                SkillPointAllocateResponseReceived?.Invoke(skillPointResponsePacket.RemainingSkillPoints);
            }
            else
            {
                Debug.LogError($"Clientside DummyServerConnection received unsupported packet: {packet.SerializeReflection()}");
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
                Scope = packet.MessageScope
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

        private void ReceiveStatFloatUpdatePacket(StatFloatUpdatePacket packet)
        {
            StatFloatUpdateReceived?.Invoke(packet.Type, packet.NewValue);
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

