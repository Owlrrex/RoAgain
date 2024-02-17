using UnityEngine;
using OwlLogging;
using System.Collections.Generic;
using Shared;
using System.IO;
using System;

// This class currently mixes DummyNetwork-logic (managing DummyClient, Receive-functions) with Game-logic.
// I'll need to split this up eventually before I introduce actual Netcode.

namespace Server
{
    public abstract class AServer
    {
        public abstract int SetupWithNewClientConnection(ClientConnection newConnection);

        public abstract IReadOnlyCollection<CharacterRuntimeData> LoggedInCharacters { get; }

        public abstract ServerMapModule MapModule { get; }

        public abstract void Update(float deltaTime);

        public abstract bool TryGetLoggedInCharacter(int characterId, out CharacterRuntimeData charData);

        public abstract void Shutdown();
    }

    public class DummyServer : AServer
    {
        // TODO: Config values
        public readonly string ACCOUNT_DB_FOLDER = Path.Combine(Application.dataPath, "AccountDb");
        public readonly string CHAR_DB_FOLDER = Path.Combine(Application.dataPath, "CharDb");

        private ServerMapModule _mapModule;
        public override ServerMapModule MapModule => _mapModule;

        private CentralConnection _centralConnection;

        private readonly List<CharacterRuntimeData> _loggedInCharacters = new();
        private IReadOnlyCollection<CharacterRuntimeData> _loggedInCharactersReadOnly;

        public override IReadOnlyCollection<CharacterRuntimeData> LoggedInCharacters => _loggedInCharactersReadOnly;

        private ChatModule _chatModule;

        private ExperienceModule _expModule;

        private AAccountDatabase _accountDatabase;

        private ACharacterDatabase _characterDatabase;

        private SkillStaticDataDatabase _skillStaticDataDatabase;

        private const float AUTOSAVE_INTERVAL = 30.0f;
        private float _autosaveTimer;

        public int Initialize()
        {
            _loggedInCharactersReadOnly = _loggedInCharacters.AsReadOnly();

            // TODO: Load/Create config values

            //int connectionInitError = InitializeDummyConnection();
            _centralConnection = new CentralConnectionImpl();

            _mapModule = new();
            _chatModule = new();
            _expModule = new();
            _centralConnection.ClientDisconnected += OnClientDisconnected;

            Configuration config = new();
            int configError = 1000000 * config.LoadConfig();

            int connectionInitError = _centralConnection.Initialize(this, "0.0.0.0:13337");

            int mapModuleError = 10 * _mapModule.Initialize(_expModule);

            int chatModuleError = 100 * _chatModule.Initialize(_mapModule, this);

            int expModuleError = 1000 * _expModule.Initialize(_mapModule);

            _accountDatabase = new AccountDatabase();
            int accountDbError = 10000 * _accountDatabase.Initialize(ACCOUNT_DB_FOLDER);

            _characterDatabase = new CharacterDatabase();
            int charDbError = 100000 * _characterDatabase.Initialize(CHAR_DB_FOLDER, _accountDatabase);

            _skillStaticDataDatabase = new();
            _skillStaticDataDatabase.Register();

            int aggregateError = connectionInitError + mapModuleError + chatModuleError + expModuleError + accountDbError + charDbError;
            return aggregateError;
        }

        private int InitializeDummyConnection()
        {
            _centralConnection = new DummyCentralConnection();
            return _centralConnection.Initialize(this, "");
        }

        public override void Shutdown()
        {
            // Before shutting down, persist all relevant data
            if(_characterDatabase != null)
            {
                foreach(CharacterRuntimeData character in _loggedInCharacters)
                {
                    _characterDatabase.Persist(character);
                }
            }

            _accountDatabase?.Persist();

            // Saving complete, shutdown systems now

            _characterDatabase?.Shutdown();

            _accountDatabase?.Shutdown();

            _centralConnection?.Shutdown();
        }

        private void ReceiveLoginAttempt(ClientConnection connection, string username, string password)
        {
            if(!_accountDatabase.AreCredentialsValid(username, password))
            {
                connection.Send(new AccountLoginResponsePacket(false));
            }
            else
            {
                connection.Send(new AccountLoginResponsePacket(true));
                connection.AccountId = username;
            }
        }

        private void ReceiveCharacterSelectionRequest(ClientConnection connection)
        {
            if(connection.AccountId == null)
            {
                OwlLogger.LogWarning($"Received CharacterSelectionRequest before successful login - dropping conection!", GameComponent.Other);
                _centralConnection.DisconnectClient(connection);
                return;
            }

            List<CharacterSelectionData> charSelList = _characterDatabase.LoadCharacterSelectionList(connection.AccountId);
            if(charSelList.Count == 0)
            {
                CharacterSelectionDataPacket charDataPacket = new()
                {
                    Count = 0,
                    Index = 0,
                    Data = null
                };
                connection.Send(charDataPacket);
            }
            else
            {
                for (int i = 0; i < charSelList.Count; i++)
                {
                    CharacterSelectionData charSelData = charSelList[i];
                    CharacterSelectionDataPacket charDataPacket = new()
                    {
                        Count = charSelList.Count,
                        Index = i,
                        Data = charSelData
                    };

                    connection.Send(charDataPacket);
                }
            }
        }

        private void ReceiveCharacterLogin(ClientConnection connection, int characterId)
        {
            if (TryGetLoggedInCharacter(characterId, out _))
            {
                OwlLogger.LogError("Tried to login character who's already logged in!", GameComponent.Other);
                CharacterLoginResponsePacket failedCharLoginPacket = new()
                {
                    Result = -1
                };
                connection.Send(failedCharLoginPacket);
                return;
            }

            CharacterRuntimeData charData = CreateAndSetupCharacterInstance(connection, characterId);
            connection.CharacterId = characterId;

            // Send main Character data
            charData.NetworkQueue.GridEntityDataUpdate(charData);

            // Send SkillTree
            List<SkillTreeEntry> skillTree = SkillTreeDatabase.GetSkillTreeForJob(charData.JobId.Value);
            foreach (SkillTreeEntry entry in skillTree)
            {
                SkillTreeEntryPacket packet = entry.ToPacket(charData);
                connection.Send(packet);
            }

            int skillPos = 0;
            int skillTier = 0;
            int maxPerRow = 5; // Arbitrary limit to how many temp skills fit into a row
            foreach (KeyValuePair<SkillId, int> kvp in charData.TemporarySkills)
            {
                if(++skillPos >= maxPerRow) 
                {
                    skillPos -= maxPerRow;
                    skillTier++;
                }
                SkillTreeEntryPacket packet = new()
                {
                    CanPointLearn = false,
                    Category = SkillCategory.Temporary,
                    LearnedSkillLvl = kvp.Value,
                    MaxSkillLvl = kvp.Value,
                    Position = skillPos,
                    SkillId = kvp.Key,
                    Tier = skillTier,
                };
                connection.Send(packet);
            }

            // TODO: Send Inventory & Equipment

            // TODO: Send buffs & debuffs

            // Send other data from the map to Client (other players, mobs, npcs, etc)
            // not needed, will be done with visibility-update or by PlaceOccupant()

            // Character will be discovered (as a normal Entity) by VisibleEntities-update, no need to inform observers

            CoroutineRunner.StartNewCoroutine(DelayedCharLoginFinish(charData, 0));
        }

        private System.Collections.IEnumerator DelayedCharLoginFinish(CharacterRuntimeData charData, int resultCode)
        {
            yield return new WaitForSeconds(0.1f); // delay some time so other packets are sent, IP-protocol ensures packets arrive in order

            CharacterLoginResponsePacket resultPacket = new()
            {
                Result = resultCode,
            };
            charData.Connection.Send(resultPacket);

            // To avoid sending visibility-related packets to the client before the CharacterLogin is completed, we place the character on the map last
            // This may cause issues for setup processes in the BattleModule (or other Modules) that expect the unit to be already placed on-grid.
            // This code was originally located in CreateAndSetupCharacterInstance()

            // Place Character on map & wherever else required
            ServerMapInstance mapInstance = _mapModule.CreateOrGetMap(charData.MapId);
            if (mapInstance == null)
            {
                OwlLogger.LogError($"Fetching Mapinstance failed for CharacterLogin id {charData.Id}, mapid {charData.MapId}!", GameComponent.Other);
                yield break;
            }

            // Setting mapid not required here, since we just got the mapinstance _from_ that name
            mapInstance.Grid.PlaceOccupant(charData, charData.Coordinates);
            charData.MapInstance = mapInstance;

            if (charData.IsDead())
                ReceivedReturnAfterDeathRequest(charData.Connection, charData.Id);
        }

        private CharacterRuntimeData CreateAndSetupCharacterInstance(ClientConnection connection, int characterId)
        {
            CharacterPersistenceData persData = _characterDatabase.LoadCharacterPersistenceData(characterId);

            // Main creation point of CharacterRuntimeData. Maybe better moved somewhere else.
            CharacterRuntimeData charData = new(connection, characterId, persData.AccountId, persData.BaseLevel, persData.JobId,
                persData.JobLevel, persData.Str, persData.Agi, persData.Vit, persData.Int, persData.Dex, persData.Luk)
            {
                // Values that are always the same (Size?!) and aren't saved
                HpRegenTime = 10,
                SpRegenTime = 5,
                Race = EntityRace.Humanoid,
                Size = EntitySize.Medium,
                Element = EntityElement.Neutral1,

                // Fields from the persistentData
                Name = persData.Name,
                MapId = persData.MapId,
                Coordinates = persData.Coordinates,
                RequiredBaseExp = _expModule.GetRequiredBaseExpOnLevel(persData.BaseLevel, false),
                CurrentBaseExp = persData.BaseExp,
                RequiredJobExp = _expModule.GetRequiredJobExpOnLevel(persData.JobLevel, persData.JobId),
                CurrentJobExp = persData.JobExp,
                RemainingStatPoints = persData.StatPoints,
                CurrentHp = persData.CurrentHP,
                CurrentSp = persData.CurrentSP,
                RemainingSkillPoints = persData.SkillPoints,
                SaveMapId = persData.SaveMapId,
                SaveCoords = persData.SaveCoords,
            };
            charData.Gender.Value = persData.Gender;
            charData.Movespeed.Value = 6; // close to default RO movespeed of 0.15 s/tile
            charData.CurrentHp = Math.Clamp(charData.CurrentHp, 0, charData.MaxHp.Total);
            charData.CurrentSp = Math.Clamp(charData.CurrentSp, 0, charData.MaxSp.Total);

            //tmp: autocorrect savepoint for old characters
            if(string.IsNullOrEmpty(charData.SaveMapId))
            {
                charData.SaveMapId = "test_map";
                charData.SaveCoords = new(5, 5);
            }

            foreach (PersistentSkillListEntry entry in persData.PermanentSkillList)
            {
                charData.PermanentSkills.Add(entry.Id, entry.Level);
            }
            // Add skills that all characters have
            charData.PermanentSkills[SkillId.AutoAttack] = 2;
            charData.PermanentSkills[SkillId.PlaceWarp] = 5;

            _loggedInCharacters.Add(charData);

            // Moved to DelayedCharLoginFinish for now: Place character on-grid

            // TODO: Register & Set up Inventory & Equipment

            // TODO: Apply Buffs/Debuffs from persistent data

            return charData;
        }

        public void ReceiveMovementRequest(ClientConnection connection, Vector2Int targetCoordinates)
        {
            if (!TryGetLoggedInCharacter(connection.CharacterId, out CharacterRuntimeData characterData))
            {
                OwlLogger.LogError($"Could not find logged in character for Id {connection.CharacterId} - dropping MovementRequest!", GameComponent.Other);
                return;
            }

            if(characterData.IsDead())
            {
                return;
            }

            if (!_mapModule.HasMapInstance(characterData.MapId))
            {
                OwlLogger.LogError($"Map {characterData.MapId} isn't initialized, cannot process Move request!", GameComponent.Other);
                return;
            }

            ServerMapInstance mapInstance = _mapModule.GetMapInstance(characterData.MapId);

            // TODO: Path Length limit
            if (mapInstance.Grid.FindAndSetPathTo(characterData, targetCoordinates) == 0)
            {
                if (characterData.QueuedSkill != null)
                {
                    OwlLogger.Log($"Cancelling queued skill {characterData.QueuedSkill.SkillId} for character {characterData.Id} by movement.", GameComponent.Other, LogSeverity.Verbose);
                    characterData.QueuedSkill = null;
                    characterData.Connection.Send(new LocalPlayerEntitySkillQueuedPacket() { SkillId = SkillId.Unknown, TargetId = -1 });
                }
            }
        }

        private void ReceiveEntitySkillRequest(ClientConnection connection, SkillId skillId, int skillLvl, int targetId)
        {
            // needing an entity-lookup for each skill isn't super efficient, but it may be ok for now.
            // create lookup-tables entityId -> mapInstance if profiling shows it's needed
            TryGetLoggedInCharacter(connection.CharacterId, out CharacterRuntimeData user);
            if(user == null)
            {
                OwlLogger.LogError($"Received EntitySkillRequest for user {connection.CharacterId} that's not logged in!", GameComponent.Skill);
                return;
            }

            ServerMapInstance map = _mapModule.GetMapInstance(user.MapId);
            if(map == null)
            {
                OwlLogger.LogError($"No map found for mapid {user.MapId} after entity lookups!", GameComponent.Other);
                return;
            }

            ServerBattleEntity target = map.Grid.FindOccupant(targetId) as ServerBattleEntity;
            if(target == null)
            {
                OwlLogger.LogWarning($"TargetId {targetId} not found on map {user.MapId} for skill!", GameComponent.Other);
                return;
            }

            map.SkillModule.ReceiveSkillExecutionRequest(skillId, skillLvl, user, new(target));
        }

        private void ReceiveGroundSkillRequest(ClientConnection connection, SkillId skillId, int skillLvl, Vector2Int target)
        {
            // needing an entity-lookup for each skill isn't super efficient, but it may be ok for now.
            // create lookup-tables entityId -> mapInstance if profiling shows it's needed
            TryGetLoggedInCharacter(connection.CharacterId, out CharacterRuntimeData user);
            if (user == null)
            {
                OwlLogger.LogError($"Received EntitySkillRequest for user {connection.CharacterId} that's not found on any map!", GameComponent.Skill);
                return;
            }

            ServerMapInstance map = _mapModule.GetMapInstance(user.MapId);
            if (map == null)
            {
                OwlLogger.LogError($"No map found for mapid {user.MapId} after entity lookups!", GameComponent.Other);
                return;
            }

            map.SkillModule.ReceiveSkillExecutionRequest(skillId, skillLvl, user, new(target));
        }

        private void ReceiveChatMessageRequest(ClientConnection connection, ChatModule.ChatMessageRequestData data)
        {
            _chatModule.HandleChatMessage(data);
        }

        // TODO: Move this to whatever system is best to handle this
        private void ReceiveStatIncreaseRequest(ClientConnection connection, EntityPropertyType statType)
        {
            if (_mapModule.FindEntityOnAllMaps(connection.CharacterId) is not CharacterRuntimeData character)
            {
                OwlLogger.LogError($"Character id {connection.CharacterId} not found on grid, but received StatIncreaseRequest.", GameComponent.Other);
                return;
            }

            Stat stat;
            int cost;
            switch(statType)
            {
                case EntityPropertyType.Str:
                    stat = character.Str;
                    cost = character.StrIncreaseCost;
                    break;
                case EntityPropertyType.Agi:
                    stat = character.Agi;
                    cost = character.AgiIncreaseCost;
                    break;
                case EntityPropertyType.Vit:
                    stat = character.Vit;
                    cost = character.VitIncreaseCost;
                    break;
                case EntityPropertyType.Int:
                    stat = character.Int;
                    cost = character.IntIncreaseCost;
                    break;
                case EntityPropertyType.Dex:
                    stat = character.Dex;
                    cost = character.DexIncreaseCost;
                    break;
                case EntityPropertyType.Luk:
                    stat = character.Luk;
                    cost = character.LukIncreaseCost;
                    break;
                default:
                    OwlLogger.LogError($"Stat {statType} not valid for StatIncreaseRequest, characterId = {character.Id}", GameComponent.Other);
                    return;
            }

            if(stat.Base >= 99)
            {
                OwlLogger.LogError($"Can't increase stat beyond 99, CharacterId = {character.Id}", GameComponent.Other);
                return;
            }
            
            if(character.RemainingStatPoints < cost)
            {
                OwlLogger.LogError($"Character {character.Id} tried to increase stat {statType} with insufficient Statpoints {character.RemainingStatPoints}, cost {cost}", GameComponent.Character);
                return;
            }

            stat.SetBase(stat.Base + 1);
            character.RemainingStatPoints -= cost;

            int newCost;
            switch (statType)
            {
                case EntityPropertyType.Str:
                    newCost = character.StrIncreaseCost;
                    break;
                case EntityPropertyType.Agi:
                    newCost = character.AgiIncreaseCost;
                    break;
                case EntityPropertyType.Vit:
                    newCost = character.VitIncreaseCost;
                    break;
                case EntityPropertyType.Int:
                    newCost = character.IntIncreaseCost;
                    break;
                case EntityPropertyType.Dex:
                    newCost = character.DexIncreaseCost;
                    break;
                case EntityPropertyType.Luk:
                    newCost = character.LukIncreaseCost;
                    break;
                default:
                    OwlLogger.LogError($"Stat {statType} not valid for StatIncreaseRequest, characterId = {character.Id}", GameComponent.Other);
                    return;
            }

            character.NetworkQueue.RemainingStatPointUpdate(character.RemainingStatPoints);
            if(cost != newCost)
            {
                character.NetworkQueue.StatCostUpdate(statType, newCost);
            }
        }

        private void ReceiveAccountCreationRequest(ClientConnection connection, string username, string password)
        {
            int result = _accountDatabase.CreateAccount(username, password);
            connection.Send(new AccountCreationResponsePacket() { Result = result });
        }

        private void ReceiveCharCreationRequest(ClientConnection connection, string charName, int gender)
        {
            if (!_characterDatabase.IsCharacterNameAvailable(charName))
            {
                connection.Send(new CharacterCreationResponsePacket() { Result = -10 });
                return;
            }

            int createResult = _characterDatabase.CreateCharacter(connection, connection.AccountId, charName, gender);
            connection.Send(new CharacterCreationResponsePacket() { Result = createResult });
        }

        private void ReceiveCharacterDeletionRequest(ClientConnection connection, int charId)
        {
            if(!_characterDatabase.DoesCharacterExist(charId))
            {
                OwlLogger.LogError($"Can't delete characterId {charId} - doesn't exist.", GameComponent.Persistence);
                connection.Send(new CharacterDeletionResponsePacket() { Result = -10 });
                return;
            }

            if(_loggedInCharacters.Find(x => x.Id == charId) != null)
            {
                OwlLogger.LogError($"Can't delete characterId {charId} while logged in!", GameComponent.Persistence);
                connection.Send(new CharacterDeletionResponsePacket() { Result = -11 });
                return;
            }

            int result = _characterDatabase.DeleteCharacter(charId);
            connection.Send(new CharacterDeletionResponsePacket() { Result = result });
        }

        private void ReceiveAccountDeletionRequest(ClientConnection connection, string accountId)
        {
            if(!_accountDatabase.DoesAccountExist(accountId))
            {
                OwlLogger.LogError($"Can't delete accountId {accountId} - doesn't exist.", GameComponent.Persistence);
                connection.Send(new AccountDeletionResponsePacket() { Result = -10 });
                return;
            }

            AccountPersistenceData accData = _accountDatabase.GetAccountData(accountId);
            foreach(int charId in accData.CharacterIds)
            {
                if(_loggedInCharacters.Find(x => x.Id == charId) != null)
                {
                    OwlLogger.LogError($"Can't delete Account {accountId} - character {charId} is logged in!", GameComponent.Persistence);
                    connection.Send(new AccountDeletionResponsePacket() { Result = -11 });
                    return;
                }
            }

            foreach(int charId in accData.CharacterIds)
            {
                int charResult = _characterDatabase.DeleteCharacter(charId);
                if(charResult != 0)
                {
                    OwlLogger.LogError($"Can't delete Account {accountId} - error while deleting character {charId}: {charResult}!", GameComponent.Persistence);
                    connection.Send(new AccountDeletionResponsePacket() { Result = -12 });
                    return;
                }
            }

            int result = _accountDatabase.DeleteAccount(accountId);
            connection.Send(new AccountDeletionResponsePacket() { Result = result });
        }

        private void ReceiveSkillPointAllocateRequest(ClientConnection connection, SkillId skillId, int amount)
        {
            SkillPointUpdatePacket responsePacket = new()
            {
                RemainingSkillPoints = -1
            };

            if (!TryGetLoggedInCharacter(connection.CharacterId, out CharacterRuntimeData characterData))
            {
                OwlLogger.LogError($"Could not find logged in character for Id {connection.CharacterId} - dropping SkillPointAllocateRequest!", GameComponent.Other);
                connection.Send(responsePacket);
                return;
            }

            responsePacket.RemainingSkillPoints = characterData.RemainingSkillPoints;

            if(characterData.RemainingSkillPoints < amount)
            {
                OwlLogger.LogError($"Character {connection.CharacterId} tried to allocate {amount} skillpoints into skill {skillId}, put only {characterData.RemainingSkillPoints} were available! Dropping request.", GameComponent.Other);
                connection.Send(responsePacket);
                return;
            }

            if(!SkillTreeDatabase.CharacterCanLearnSkill(characterData, skillId))
            {
                OwlLogger.LogError($"Character {connection.CharacterId} does not qualify to learn skill {skillId}!", GameComponent.Other);
                connection.Send(responsePacket);
                return;
            }

            List<SkillTreeEntry> skillTree = SkillTreeDatabase.GetSkillTreeForJob(characterData.JobId.Value);
            SkillTreeEntry skillEntry = null;
            foreach(SkillTreeEntry entry in skillTree)
            {
                if (entry.Skill == skillId)
                {
                    skillEntry = entry;
                    break;
                }
            }

            if (skillEntry == null)
            {
                OwlLogger.LogError($"Character {connection.CharacterId} doens't have skill {skillId} in its skill tree - can't allocate points.", GameComponent.Other);
                connection.Send(responsePacket);
                return;
            }

            int alreadyAlloctedPoints = 0;
            if(characterData.PermanentSkills.ContainsKey(skillId))
            {
                alreadyAlloctedPoints = characterData.PermanentSkills[skillId];
            }
            if(alreadyAlloctedPoints + amount > skillEntry.MaxLevel)
            {
                OwlLogger.LogError($"Character {connection.CharacterId} tried to allocate {amount} skill points into skill {skillId} with {alreadyAlloctedPoints} already placed - breaking limit of skill level {skillEntry.MaxLevel}!", GameComponent.Other);
                connection.Send(responsePacket);
                return;
            }

            characterData.RemainingSkillPoints -= amount;
            responsePacket.RemainingSkillPoints = characterData.RemainingSkillPoints;

            if (characterData.PermanentSkills.ContainsKey(skillId))
            {
                characterData.PermanentSkills[skillId] += amount;
            }
            else
            {
                characterData.PermanentSkills[skillId] = amount;
            }

            SkillTreeEntryPacket packet = skillEntry.ToPacket(characterData);
            connection.Send(packet);
            connection.Send(responsePacket);
        }

        private void ReceivedReturnAfterDeathRequest(ClientConnection connection, int characterId)
        {
            if (!TryGetLoggedInCharacter(characterId, out CharacterRuntimeData charData))
            {
                OwlLogger.LogError($"Received ReturnTosave for characterId {characterId} that wasn't logged in!", GameComponent.Other);
                return;
            }

            if (ReturnCharacterToSavePoint(charData) == 0)
            {
                // TODO: Configurable amount of heal after death

                _mapModule.GetMapInstance(charData.MapId)?.BattleModule?.ChangeHp(charData, (int)(charData.MaxHp.Total * 0.5f), charData);
            }
        }

        private int ReturnCharacterToSavePoint(CharacterRuntimeData charData)
        {
            if(charData == null)
            {
                OwlLogger.LogError($"Tried to return null character to save point", GameComponent.Other);
                return -1;
            }

            if (string.IsNullOrEmpty(charData.SaveMapId) || charData.SaveCoords == GridData.INVALID_COORDS)
            {
                OwlLogger.LogError($"Tried to return character {charData.Id} to save point, but its savepoint isn't set!", GameComponent.Other);
                return -2;
            }

            return ServerMain.Instance.Server.MapModule.MoveEntityBetweenMaps(charData.Id, charData.MapId, charData.SaveMapId, charData.SaveCoords);
        }

        private void ReceivedCharacterLogoutRequest(ClientConnection connection)
        {
            if(connection.CharacterId < 0)
            {
                OwlLogger.LogError("Received CharacterLogoutRequest for connection that didn't have a characterId set!", GameComponent.Other);
                return;
            }

            DisconnectCharacter(connection.CharacterId);
        }

        public override int SetupWithNewClientConnection(ClientConnection newConnection)
        {
            if(newConnection == null)
            {
                OwlLogger.LogError("Cannot setup Server with null ClientConnection!", GameComponent.Other);
                return -1;
            }

            newConnection.LoginRequestRecieved += ReceiveLoginAttempt;
            newConnection.CharacterLoginReceived += ReceiveCharacterLogin;
            newConnection.CharacterSelectionRequestReceived += ReceiveCharacterSelectionRequest;
            newConnection.MovementRequestReceived += ReceiveMovementRequest;
            newConnection.EntitySkillRequestReceived += ReceiveEntitySkillRequest;
            newConnection.GroundSkillRequestReceived += ReceiveGroundSkillRequest;
            newConnection.ChatMessageRequestReceived += ReceiveChatMessageRequest;
            newConnection.StatIncreaseRequestReceived += ReceiveStatIncreaseRequest;
            newConnection.AccountCreationRequestReceived += ReceiveAccountCreationRequest;
            newConnection.AccountDeletionRequestReceived += ReceiveAccountDeletionRequest;
            newConnection.CharacterCreationRequestReceived += ReceiveCharCreationRequest;
            newConnection.CharacterDeletionRequestReceived += ReceiveCharacterDeletionRequest;
            newConnection.SkillPointAllocateRequestReceived += ReceiveSkillPointAllocateRequest;
            newConnection.ReturnAfterDeathRequestReceived += ReceivedReturnAfterDeathRequest;
            newConnection.CharacterLogoutRequestReceived += ReceivedCharacterLogoutRequest;
            
            return 0;
        }

        private void OnClientDisconnected(int characterId)
        {
            DisconnectCharacter(characterId);

            // ClientConnection was cleaned up by the CentralConnection already
        }

        private void DisconnectCharacter(int characterId)
        {
            if (!TryGetLoggedInCharacter(characterId, out CharacterRuntimeData charData))
            {
                OwlLogger.LogError($"Received CharacterDisconnected for characterId {characterId} that wasn't logged in!", GameComponent.Other);
                return;
            }

            // Get Map Instances for character-Id
            ServerMapInstance mapInstance = charData.MapInstance;

            // Tell MapInstance to remove character from Grid
            // This should automatically send the EntityRemoved event (only to people in range?)
            mapInstance.Grid.RemoveOccupant(charData);

            _loggedInCharacters.Remove(charData);

            charData.Connection.CharacterId = -1;
        }

        public override void Update(float deltaTime)
        {
            _mapModule?.Update(deltaTime);

            foreach (CharacterRuntimeData character in _loggedInCharacters)
            {
                character.NetworkQueue.Update(deltaTime);
            }

            _centralConnection?.Update();

            _autosaveTimer += deltaTime;
            if(_autosaveTimer >= AUTOSAVE_INTERVAL)
            {
                foreach (CharacterRuntimeData character in _loggedInCharacters)
                {
                    _characterDatabase.Persist(character);
                }
                _autosaveTimer -= AUTOSAVE_INTERVAL;
            }
        }

        public override bool TryGetLoggedInCharacter(int characterId, out CharacterRuntimeData charData)
        {
            charData = _loggedInCharacters.Find(item => item.Id == characterId);
            return charData != null;
        }
    }
}

