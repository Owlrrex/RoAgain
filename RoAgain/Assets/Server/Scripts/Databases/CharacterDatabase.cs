using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Server
{
    [Serializable]
    public class PersistentSkillListEntry
    {
        public SkillId Id;
        public int Level;
    }

    [Serializable]
    public class CharacterPersistenceData
    {
        public int CharacterId = -1;
        public string AccountId;
        public string Name;
        public string MapId;
        public Vector2Int Coordinates;
        public int Gender;
        public int BaseLevel = -1;
        public int BaseExp = -1;
        public JobId JobId = JobId.Unknown;
        public int JobLevel = -1;
        public int JobExp = -1;
        public int StatPoints = -1;
        public int Str = -1;
        public int Agi = -1;
        public int Vit = -1;
        public int Int = -1;
        public int Dex = -1;
        public int Luk = -1;
        public int CurrentHP = -1;
        public int CurrentSP = -1;

        // TODO: Equip
        // TODO: Inventory

        public List<PersistentSkillListEntry> PermanentSkillList = new();

        public int SkillPoints = -1;

        public string SaveMapId;
        public Vector2Int SaveCoords;

        public DictionarySerializationWrapper<int, int> CharConfig = new();

        public static CharacterPersistenceData FromRuntimeData(CharacterRuntimeData runtimeData)
        {
            if (runtimeData == null)
            {
                OwlLogger.LogError("Can't create CharacterPersistenceData for null RuntimeData", GameComponent.Persistence);
                return null;
            }

            CharacterPersistenceData data = new()
            {
                CharacterId = runtimeData.CharacterId,
                AccountId = runtimeData.AccountId,
                Name = runtimeData.NameOverride,
                BaseLevel = runtimeData.BaseLvl.Value,
                BaseExp = runtimeData.CurrentBaseExp,
                JobId = runtimeData.JobId,
                JobLevel = runtimeData.JobLvl.Value,
                JobExp = runtimeData.CurrentJobExp,
                Str = runtimeData.Str.Base,
                Agi = runtimeData.Agi.Base,
                Vit = runtimeData.Vit.Base,
                Int = runtimeData.Int.Base,
                Dex = runtimeData.Dex.Base,
                Luk = runtimeData.Luk.Base,
                CurrentHP = runtimeData.CurrentHp,
                CurrentSP = runtimeData.CurrentSp,
                StatPoints = runtimeData.RemainingStatPoints,
                MapId = runtimeData.MapId,
                Coordinates = runtimeData.Coordinates,
                SkillPoints = runtimeData.RemainingSkillPoints,
                Gender = runtimeData.Gender.Value,
                SaveMapId = runtimeData.SaveMapId,
                SaveCoords = runtimeData.SaveCoords,
            };

            foreach(KeyValuePair<SkillId, int> kvp in runtimeData.PermanentSkills)
            {
                data.PermanentSkillList.Add(new() { Id = kvp.Key, Level = kvp.Value });
            }

            return data;
        }
    }

    public abstract class ACharacterDatabase
    {
        protected AAccountDatabase _accountDatabase;
        protected int _nextCharId = 10000;

        public abstract int Initialize(string config, AAccountDatabase accountDatabase);

        public abstract void Shutdown();

        public abstract int Persist(CharacterRuntimeData characterData);

        public abstract int Persist(CharacterPersistenceData characterData);

        public abstract bool DoesCharacterExist(int characterId);

        public abstract CharacterPersistenceData LoadCharacterPersistenceData(int characterId);

        public abstract bool IsCharacterNameAvailable(string characterName);

        public abstract int DeleteCharacter(int characterId);

        public virtual int CreateCharacter(ClientConnection connection, string accountId, string name, int gender)
        {
            int newCharacterId = GetNextCharacterId();
            if (DoesCharacterExist(newCharacterId))
            {
                OwlLogger.LogError($"Can't create character with id {newCharacterId} - already exists!", GameComponent.Persistence);
                return -1;
            }

            AccountPersistenceData acct = _accountDatabase.GetAccountData(accountId);
            if (acct == null)
            {
                OwlLogger.LogError($"Can't create character for account {accountId} that doesn't exist!", GameComponent.Persistence);
                return -2;
            }

            if (acct.CharacterIds.Contains(newCharacterId))
            {
                OwlLogger.LogError($"Can't create character id {newCharacterId} on account {accountId} - id already exists!", GameComponent.Persistence);
                return -3;
            }

            if (!IsCharacterNameAvailable(name))
            {
                OwlLogger.LogError($"Can't create character with name {name} - already exists.", GameComponent.Persistence);
                return -4;
            }

            OwlLogger.Log($"Creating character {name} for account {accountId}", GameComponent.Persistence);

            CharacterPersistenceData charPersData;
            // Create default persistence data
            charPersData = new()
            {
                CharacterId = newCharacterId,
                AccountId = accountId,
                Name = name,
                Gender = gender,
                BaseLevel = 1,
                BaseExp = 0,
                JobId = JobId.Novice,
                JobLevel = 1,
                JobExp = 0,
                Coordinates = new(5,5), // TODO: Better system for a starting-pos
                Str = 1,
                Agi = 1,
                Vit = 1,
                Int = 1,
                Dex = 1,
                Luk = 1,
                SkillPoints = 0,
                StatPoints = 44, // TODO: Config-value for this
                CurrentHP = 99999,
                CurrentSP = 9999,
                MapId = "test_map", // TODO: Better system for a starting-pos
                SaveMapId = "test_map", // TODO: Better system for initial save point
                SaveCoords = new(5, 5)
            };

            int charPersistResult = Persist(charPersData);
            if(charPersistResult != 0)
            {
                OwlLogger.LogError($"Error code {charPersistResult} while persisting new character!", GameComponent.Persistence);
                return charPersistResult * 10;
            }

            // Add characterId to AccountId
            acct.CharacterIds.Add(newCharacterId);
            _accountDatabase.Persist(accountId);

            return 0;
        }

        protected int GetNextCharacterId()
        {
            return _nextCharId++;
        }

        public List<CharacterSelectionData> LoadCharacterSelectionList(string accountId)
        {
            if(!_accountDatabase.DoesAccountExist(accountId))
            {
                OwlLogger.LogError($"Can't get CharacterSelectionList for account {accountId} - account doesn't exist.", GameComponent.Persistence);
                return null;
            }

            AccountPersistenceData acct = _accountDatabase.GetAccountData(accountId);
            List<CharacterSelectionData> list = new(acct.CharacterIds.Count);
            foreach (int charId in acct.CharacterIds)
            {
                CharacterPersistenceData charPersData = LoadCharacterPersistenceData(charId);
                CharacterSelectionData charSelData = new()
                {
                    CharacterId = charPersData.CharacterId,
                    Name = charPersData.Name,
                    MapId = charPersData.MapId,
                    JobId = charPersData.JobId,
                    JobLevel = charPersData.JobLevel,
                    BaseLevel = charPersData.BaseLevel,
                    BaseExp = charPersData.BaseExp,
                    //Hp = charPersData.
                    //Sp = charPersData.
                    Str = charPersData.Str,
                    Agi = charPersData.Agi,
                    Vit = charPersData.Vit,
                    Int = charPersData.Int,
                    Dex = charPersData.Dex,
                    Luk = charPersData.Luk,
                };

                list.Add(charSelData);
            }
            
            return list;
        }

        public abstract bool GetConfigValue(int characterId, int configKey, out int value);

        public abstract void SetConfigValue(int characterId, int configKey, int value);
    }

    // The file-based format has to re-save the whole character every time. This may cause scaling issues.
    // We need a storage format that can save individual characters, otherwise we'll be in deep trouble.
    public class CharacterDatabase : ACharacterDatabase
    {
        private string _folderPath;
        private HashSet<string> _usedCharNames = new();
        private Dictionary<int, RemoteConfigStorage> _storedCharConfigs = new();

        public override int Initialize(string config, AAccountDatabase accountDatabase)
        {
            if (string.IsNullOrEmpty(config))
            {
                OwlLogger.LogError($"Can't initialize CharacterDatabase with empty config", GameComponent.Persistence);
                return -1;
            }

            if (accountDatabase == null)
            {
                OwlLogger.LogError("Can't initialize CharacterDatabase with empty AccountDatabase!", GameComponent.Persistence);
                return -2;
            }

            if (!Directory.Exists(config))
            {
                OwlLogger.Log($"Creating Character database folder: {config}", GameComponent.Persistence);
                try
                {
                    Directory.CreateDirectory(config);
                }
                catch (Exception e)
                {
                    OwlLogger.LogError($"Exception while creating Character Database folder at {config}: {e.Message}", GameComponent.Persistence);
                    return -3;
                }
            }

            _accountDatabase = accountDatabase;
            _folderPath = config;

            // Find the highest used characterId & used names
            OwlLogger.Log("Scanning CharacterDatabase for used CharacterIds & -names", GameComponent.Persistence);
            int charCount = 0;
            string[] files;
            try
            {
                files = Directory.GetFiles(_folderPath, "*.chardb");
            }
            catch(Exception e)
            {
                OwlLogger.LogError($"Exception while accessing files in Character Database: {e.Message}", GameComponent.Persistence);
                return -4;
            }
            
            foreach (string file in files)
            {
                string filename = Path.GetFileNameWithoutExtension(file);
                if (!int.TryParse(filename, out int charId))
                {
                    OwlLogger.LogError($"Character file {file} doesn't have parseable characterId!", GameComponent.Persistence);
                    continue;
                }
                _nextCharId = Math.Max(_nextCharId, charId+1);
                charCount++;

                CharacterPersistenceData charData = LoadCharacterPersistenceData(charId);
                _usedCharNames.Add(charData.Name);
            }
            OwlLogger.LogF("Scanned {0} characters, next characterId is {1}", charCount, _nextCharId, GameComponent.Persistence);

            return 0;
        }

        public override void Shutdown()
        {
            _usedCharNames.Clear();
            _storedCharConfigs.Clear();
        }

        private string MakeFilePathForCharacter(int characterId)
        {
            return Path.Combine(_folderPath, $"{characterId}.chardb");
        }

        public override int Persist(CharacterRuntimeData charData)
        {
            if(charData == null)
            {
                OwlLogger.LogError("Can't persist null character!", GameComponent.Persistence);
                return -2;
            }

            if(charData.CharacterId <= 0)
            {
                OwlLogger.LogError($"Can't persist invalid characterId {charData.CharacterId}", GameComponent.Persistence);
                return -1;
            }

            CharacterPersistenceData fullData = CharacterPersistenceData.FromRuntimeData(charData);

            return Persist(fullData);
        }

        public override int Persist(CharacterPersistenceData charData)
        {
            if (charData == null)
            {
                OwlLogger.LogError("Can't persist null character!", GameComponent.Persistence);
                return -1;
            }

            if (charData.CharacterId <= 0)
            {
                OwlLogger.LogError($"Can't persist invalid characterId {charData.CharacterId}", GameComponent.Persistence);
                return -2;
            }

            if(string.IsNullOrEmpty(charData.AccountId))
            {
                OwlLogger.LogError($"Can't persist character {charData.CharacterId} without AccountId!", GameComponent.Persistence);
                return -3;
            }

            if (charData.Coordinates == GridData.INVALID_COORDS)
            {
                OwlLogger.LogError($"Can't persist character with invalid coordinates!", GameComponent.Persistence);
                return -4;
            }

            string path = MakeFilePathForCharacter(charData.CharacterId);

            if(_storedCharConfigs.TryGetValue(charData.CharacterId, out RemoteConfigStorage storage))
            {
                charData.CharConfig.FromDict(storage.Values);
            }

            string data = JsonUtility.ToJson(charData);
            try
            {
                File.WriteAllText(path, data);
            }
            catch (Exception e)
            {
                OwlLogger.LogError($"Exception while persisting character {charData.CharacterId}: {e.Message}", GameComponent.Persistence);
                return -5;
            }

            return 0;
        }

        public override bool DoesCharacterExist(int characterId)
        {
            return File.Exists(MakeFilePathForCharacter(characterId));
        }

        public override CharacterPersistenceData LoadCharacterPersistenceData(int characterId)
        {
            if (!DoesCharacterExist(characterId))
            {
                OwlLogger.LogError($"Can't load character data for id {characterId} - character not found", GameComponent.Persistence);
                return null;
            }

            string filePath = MakeFilePathForCharacter(characterId);

            string rawData;
            try
            {
                rawData = File.ReadAllText(filePath);
            }
            catch
            {
                OwlLogger.LogError($"Failed to load character file {filePath}", GameComponent.Persistence);
                return null;
            }

            CharacterPersistenceData data = JsonUtility.FromJson<CharacterPersistenceData>(rawData);

            _storedCharConfigs[data.CharacterId] = new(data.CharConfig.ToDict());
            return data;
        }

        private void OnCharacterLogout(int characterId)
        {
            _storedCharConfigs.Remove(characterId);
        }

        public override bool IsCharacterNameAvailable(string characterName)
        {
            return !_usedCharNames.Contains(characterName);
        }

        public override int CreateCharacter(ClientConnection connection, string accountId, string name, int gender)
        {
            int result = base.CreateCharacter(connection, accountId, name, gender);
            if(result == 0)
                _usedCharNames.Add(name);
            return result;
        }

        public override int DeleteCharacter(int characterId)
        {
            if (!DoesCharacterExist(characterId))
            {
                OwlLogger.LogError($"Can't delete characterId {characterId} - doesn't exist.", GameComponent.Persistence);
                return -1;
            }

            CharacterPersistenceData charData = LoadCharacterPersistenceData(characterId);
            _usedCharNames.Remove(charData.Name);

            AccountPersistenceData acctData = _accountDatabase.GetAccountData(charData.AccountId);
            acctData.CharacterIds.Remove(characterId);
            _accountDatabase.Persist(charData.AccountId);

            string path = MakeFilePathForCharacter(characterId);
            try
            {
                File.Delete(path);
            }
            catch(Exception ex)
            {
                OwlLogger.LogError($"Exception while deleting character {characterId}: {ex.Message}", GameComponent.Persistence);
                return -2;
            }

            return 0;
        }

        public override bool GetConfigValue(int characterId, int configKey, out int value)
        {
            if(!_storedCharConfigs.TryGetValue(characterId, out RemoteConfigStorage storage))
            {
                value = 0;
                return false;
            }

            value = storage.GetConfigValue(configKey);
            return true;
        }

        public override void SetConfigValue(int characterId, int configKey, int value)
        {
            if(!_storedCharConfigs.TryGetValue(characterId, out RemoteConfigStorage storage))
            {
                storage = new(null);
                _storedCharConfigs.Add(characterId, storage);
            }

            storage.SetConfigValue(configKey, value);
        }
    }
}


