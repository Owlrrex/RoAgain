using OwlLogging;
using Shared;
using System.Collections.Generic;

namespace Server
{
    public class NetworkQueue
    {
        private ClientConnection _connection;

        private Dictionary<int, GridEntity> _pathUpdates = new();
        private Dictionary<int, GridEntity> _gridEntityUpdates = new();
        private CharacterRuntimeData _localCharacterUpdate;
        private Dictionary<EntityPropertyType, Stat> _statUpdates = new();
        private Dictionary<EntityPropertyType, int> _statCostUpdates = new();
        private int _newRemainingStatPoints = -1;
        private Dictionary<int, ServerBattleEntity> _hpUpdates = new();
        private Dictionary<int, ServerBattleEntity> _spUpdates = new();
        private CharacterRuntimeData _expUpdate;
        private Dictionary<int, ServerBattleEntity> _baseLevelUps = new();
        private Dictionary<int, CharacterRuntimeData> _jobLevelUps = new();
        private Dictionary<int, PickupEntity> _pickupsRemoved = new();

        public int Initialize(ClientConnection connection)
        {
            if(connection == null)
            {
                OwlLogger.LogError("Can't initialize NetworkQueue with null ClientConnection!", GameComponent.Network);
                return -1;
            }

            _connection = connection;
            return 0;
        }

        public void Update(float deltaTime)
        {
            TryMergeData();

            foreach(GridEntity entity in _pathUpdates.Values)
            {
                _connection.Send(new EntityPathUpdatePacket()
                { 
                    UnitId = entity.Id,
                    Path = entity.Path
                });
            }

            foreach(GridEntity entity in _gridEntityUpdates.Values)
            {
                if (entity.Coordinates == GridData.INVALID_COORDS) // entity got removed since it was queued, ignore
                    continue;

                _connection.Send(entity.ToDataPacket());
            }

            if (_localCharacterUpdate != null)
            {
                _connection.Send(_localCharacterUpdate.ToLocalDataPacket());
            }

            foreach(KeyValuePair<EntityPropertyType, Stat> kvp in _statUpdates)
            {
                _connection.Send(new StatUpdatePacket()
                {
                    Type = kvp.Key,
                    NewValue = kvp.Value,
                });
            }

            foreach(KeyValuePair<EntityPropertyType, int> kvp in _statCostUpdates)
            {
                _connection.Send(new StatCostUpdatePacket()
                {
                    Type = kvp.Key,
                    NewCost = kvp.Value,
                });
            }

            if(_newRemainingStatPoints != -1)
            {
                _connection.Send(new StatPointUpdatePacket()
                {
                    NewRemaining = _newRemainingStatPoints,
                });
            }

            foreach(ServerBattleEntity entity in _hpUpdates.Values)
            {
                _connection.Send(new HpUpdatePacket()
                {
                    EntityId = entity.Id,
                    NewHp = entity.CurrentHp
                });
            }

            foreach(ServerBattleEntity entity in _spUpdates.Values)
            {
                _connection.Send(new SpUpdatePacket()
                {
                    EntityId = entity.Id,
                    NewSp = entity.CurrentSp
                });
            }

            if(_expUpdate != null)
            {
                _connection.Send(new ExpUpdatePacket()
                {
                    CurrentBaseExp = _expUpdate.CurrentBaseExp,
                    CurrentJobExp = _expUpdate.CurrentJobExp,
                });
            }

            foreach(ServerBattleEntity entity in _baseLevelUps.Values)
            {
                _connection.Send(new LevelUpPacket()
                {
                    EntityId = entity.Id,
                    IsJob = false,
                    Level = entity.BaseLvl.Value,
                    RequiredExp = entity is CharacterRuntimeData character ? character.RequiredBaseExp : 0,
                });
            }

            foreach(CharacterRuntimeData character in _jobLevelUps.Values)
            {
                _connection.Send(new LevelUpPacket()
                {
                    EntityId = character.Id,
                    IsJob = true,
                    Level = character.JobLvl.Value,
                    RequiredExp = character.RequiredJobExp,
                });
            }

            foreach(PickupEntity pickup in _pickupsRemoved.Values)
            {
                PickupRemovedPacket packet = new()
                {
                    PickupId = pickup.Id
                };
                packet.PickedUpEntityId = pickup.State switch
                {
                    PickupState.AboutToDisappear => PickupRemovedPacket.PICKUP_ENTITY_TIMEOUT,
                    PickupState.PickedUp => pickup.OwnerEntityId,
                    _ => PickupRemovedPacket.PICKUP_ENTITY_VISION
                };
                _connection.Send(packet);
            }

            _pathUpdates.Clear();
            _gridEntityUpdates.Clear();
            _statUpdates.Clear();
            _hpUpdates.Clear();
            _spUpdates.Clear();
            _localCharacterUpdate = null;
            _newRemainingStatPoints = -1;
            _expUpdate = null;
            _baseLevelUps.Clear();
            _jobLevelUps.Clear();
        }

        private void TryMergeData()
        {

        }

        public void GridEntityPathUpdate(GridEntity entity)
        {
            OwlLogger.Log($"EntityPathUpdatePacket queued for entity {entity.Id}", GameComponent.Network, LogSeverity.VeryVerbose);

            if((_localCharacterUpdate != null && entity.Id == _localCharacterUpdate.Id)
                || _gridEntityUpdates.ContainsKey(entity.Id))
            {
                // Could also modify the path in the data packet, if that creates better behaviour
                // Simply observe when this warning appears & decide then.
                OwlLogger.LogWarning($"Dropping EntityPathUpdate for entity {entity.Id} to player {_connection.CharacterId} - entityData packet already queued!", GameComponent.Network);
                return;
            }

            _pathUpdates[entity.Id] = entity;
        }

        public void GridEntityDataUpdate(GridEntity entity)
        {
            if (entity is CharacterRuntimeData charData)
            {
                if(charData.CharacterId == _connection.CharacterId)
                {
                    OwlLogger.Log($"LocalCharacterDataPacket queued for entity {entity.Id}", GameComponent.Network, LogSeverity.VeryVerbose);

                    _localCharacterUpdate = charData;

                    // Remove Stat packets, since they'll also be contained
                    // All stat updates are necessarily for the local character (at the moment), so no need to filter them for Ids
                    _statUpdates.Clear();
                    return;
                }
            }

            OwlLogger.Log($"GridEntityDataPacket queued for entity {entity.Id}", GameComponent.Network, LogSeverity.VeryVerbose);
            _gridEntityUpdates[entity.Id] = entity;

            _pathUpdates.Remove(entity.Id);
        }

        public void StatUpdate(EntityPropertyType type, Stat newValue)
        {
            if(type == EntityPropertyType.Unknown)
            {
                OwlLogger.LogError($"Can't queue unknown statType, characterId {_connection.CharacterId}", GameComponent.Network);
                return;
            }

            if(newValue == null)
            {
                OwlLogger.LogError($"Can't queue null stat for type {type}, characterId {_connection.CharacterId}", GameComponent.Network);
                return;
            }

            if(_localCharacterUpdate != null)
            {
                OwlLogger.Log($"Dropping StatUpdate for player {_connection.CharacterId} - localCharData packet already queued!", GameComponent.Network);
                return;
            }

            _statUpdates[type] = newValue;
        }

        public void StatCostUpdate(EntityPropertyType type, int newCost)
        {
            if (type == EntityPropertyType.Unknown)
            {
                OwlLogger.LogError($"Can't queue unknown statType, characterId {_connection.CharacterId}", GameComponent.Network);
                return;
            }

            if (_localCharacterUpdate != null)
            {
                OwlLogger.Log($"Dropping StatCostUpdate for player {_connection.CharacterId} - localCharData packet already queued!", GameComponent.Network);
                return;
            }

            _statCostUpdates[type] = newCost;
        }

        public void RemainingStatPointUpdate(int newRemaining)
        {
            _newRemainingStatPoints = newRemaining;
        }

        public void HpUpdate(ServerBattleEntity entity)
        {
            if(entity == null)
            {
                OwlLogger.LogError($"Can't queue HpUpdate for null entity!", GameComponent.Network);
                return;
            }

            if(_gridEntityUpdates.ContainsKey(entity.Id)
                || (_localCharacterUpdate != null && _localCharacterUpdate.Id == entity.Id))
            {
                OwlLogger.Log($"Dropping HpUpdate for entity {entity.Id} - data packet already queued.", GameComponent.Network);
                return;
            }

            _hpUpdates[entity.Id] = entity;
        }

        public void SpUpdate(ServerBattleEntity entity)
        {
            if (entity == null)
            {
                OwlLogger.LogError($"Can't queue SpUpdate for null entity!", GameComponent.Network);
                return;
            }

            if (_gridEntityUpdates.ContainsKey(entity.Id)
                || (_localCharacterUpdate != null && _localCharacterUpdate.Id == entity.Id))
            {
                OwlLogger.Log($"Dropping SpUpdate for entity {entity.Id} - data packet already queued.", GameComponent.Network);
                return;
            }

            _spUpdates[entity.Id] = entity;
        }

        public void ExpUpdate(CharacterRuntimeData character)
        {
            if(character == null)
            {
                OwlLogger.LogError("Can't queue ExpUpdate for null character", GameComponent.Network);
                return;
            }

            _expUpdate = character;
        }

        public void BaseLevelUp(ServerBattleEntity entity)
        {
            if(entity == null)
            {
                OwlLogger.LogError("Can't queue BaseLevelUp for null entity", GameComponent.Network);
                return;
            }

            _baseLevelUps[entity.Id] = entity;
        }

        public void JobLevelUp(CharacterRuntimeData character)
        {
            if(character == null)
            {
                OwlLogger.LogError("Can't queue JobLevelUp for null character", GameComponent.Network);
                return;
            }

            _jobLevelUps[character.Id] = character;
        }

        public void PickupRemoved(PickupEntity pickup)
        {
            if(pickup == null)
            {
                OwlLogger.LogError("Can't queue PickupRemoved for null pickup!", GameComponent.Network);
                return;
            }

            _pickupsRemoved[pickup.Id] = pickup;

            _gridEntityUpdates.Remove(pickup.Id); // Don't send updates for removed pickups
        }
    }
}

