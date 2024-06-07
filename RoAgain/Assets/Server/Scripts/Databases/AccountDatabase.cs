using OwlLogging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Shared;

namespace Server
{
    [Serializable]
    public class AccountPersistenceData
    {
        public string Username;
        public string PasswordHash;
        public List<int> CharacterIds = new();
        public DictionarySerializationWrapper<int, int> AccountConfig = new();
    }

    public abstract class AAccountDatabase
    {
        protected Regex _accountNameRegex = new("^[a-zA-Z0-9]*$");
        //protected Regex _passwordRegex = new("");

        protected Dictionary<string, AccountPersistenceData> _accounts = new();

        public abstract int Initialize(string config);

        public abstract void Shutdown();

        public abstract int Persist();

        public abstract int Persist(string accountId);

        public abstract int DeleteAccount(string accountId);

        public bool AreCredentialsValid(string username, string password)
        {
            if (!DoesAccountExist(username))
                return false;

            string pwHash = Hash(password);
            return pwHash == _accounts[username].PasswordHash;
        }

        public virtual int CreateAccount(string username, string password)
        {
            if (!IsValidUsername(username))
            {
                OwlLogger.Log($"Account creation rejected - invalid username", GameComponent.Persistence);
                return 3;
            }

            if(DoesAccountExist(username))
            {
                OwlLogger.Log($"Account creation rejected - username already taken.", GameComponent.Persistence);
                return 2;
            }

            if(!IsValidPassword(password))
            {
                OwlLogger.Log($"Account creation rejected - invalid Password.", GameComponent.Persistence);
                return 1;
            }

            OwlLogger.Log($"Creating account {username}", GameComponent.Persistence);

            string pwHash = Hash(password);
            AccountPersistenceData account = new()
            {
                Username = username,
                PasswordHash = pwHash,
            };

            _accounts.Add(username, account);

            Persist(username);
            return 0;
        }

        public bool DoesAccountExist(string accountId)
        {
            return _accounts.ContainsKey(accountId);
        }

        public AccountPersistenceData GetAccountData(string username)
        {
            if(!_accounts.ContainsKey(username))
            {
                OwlLogger.LogError($"Can't get AccountData for account {username} that doesn't exist!", GameComponent.Persistence);
                return null;
            }

            return _accounts[username]; 
        }

        private string Hash(string data)
        {
            using SHA256 sha = SHA256.Create();
            return Encoding.UTF8.GetString(sha.ComputeHash(Encoding.UTF8.GetBytes(data)));
        }

        public bool IsValidUsername(string username)
        {
            return !string.IsNullOrEmpty(username)
                && username.Length > 3
                && _accountNameRegex.IsMatch(username);
        }

        public bool IsValidPassword(string password)
        {
            return !string.IsNullOrEmpty(password)
                && password.Length > 6;
        }

        public abstract bool GetConfigValue(string accountId, int configKey, out int value);

        public abstract void SetConfigValue(string accountId, int configKey, int value);
    }

    public class AccountDatabase : AAccountDatabase
    {
        private string _folderPath;
        private Dictionary<string, RemoteConfigStorage> _storedAccConfigs = new();

        public override int Initialize(string config)
        {
            if (string.IsNullOrEmpty(config))
            {
                OwlLogger.LogError($"Can't initialize AccountDatabase with empty config", GameComponent.Persistence);
                return -1;
            }

            if (!Directory.Exists(config))
            {
                OwlLogger.Log($"Creation Account database folder: {config}", GameComponent.Persistence);
                try
                {
                    Directory.CreateDirectory(config);
                }
                catch (Exception e)
                {
                    OwlLogger.LogError($"Exception while creating Account Database folder at {config}: {e.Message}", GameComponent.Persistence);
                    return -2;
                }
            }

            _folderPath = config;
            int loadError = LoadAccountData();

            return loadError * 10;
        }

        public override void Shutdown()
        {
            Persist();
            _storedAccConfigs.Clear();
        }

        private int LoadAccountData()
        {
            string rawData;
            string[] accFiles;
            try
            {
                accFiles = Directory.GetFiles(_folderPath, "*.accdb", SearchOption.TopDirectoryOnly);
            }
            catch (Exception e)
            {
                OwlLogger.LogError($"Exception while accessing files in Account Database: {e.Message}", GameComponent.Persistence);
                return -1;
            }

            foreach (string accFile in accFiles)
            {
                try
                {
                    rawData = File.ReadAllText(accFile);
                }
                catch (Exception e)
                {
                    OwlLogger.LogError($"Exception while reading Account Database file {accFile}: {e.Message}", GameComponent.Persistence);
                    continue;
                }

                if (string.IsNullOrEmpty(rawData))
                {
                    OwlLogger.LogError($"AccountDatabase rawData is empty!", GameComponent.Persistence);
                    continue;
                }

                AccountPersistenceData persData = JsonUtility.FromJson<AccountPersistenceData>(rawData);
                if(persData.Username != Path.GetFileNameWithoutExtension(accFile))
                {
                    OwlLogger.LogError($"AccountData for username {persData.Username} found in file with different filename. Path = {accFile}", GameComponent.Persistence);
                    continue;
                }
                _accounts[persData.Username] = persData;
                _storedAccConfigs[persData.Username] = new(persData.AccountConfig.ToDict());
            }

            return 0;
        }

        public override int Persist()
        {
            int result = 0;
            foreach(var kvp in _accounts)
            {
                result = Math.Min(result, Persist(kvp.Key));
            }
            return result;
        }

        public override int Persist(string accountId)
        {
            string pathToFile = MakeFilePathForAccount(accountId);
            if (!_accounts.ContainsKey(accountId))
            {
                OwlLogger.LogError($"Can't persist accountId {accountId} that's not managed by AccountDb!", GameComponent.Persistence);
                return -1;
            }

            if (_storedAccConfigs.TryGetValue(accountId, out RemoteConfigStorage storage))
            {
                _accounts[accountId].AccountConfig.FromDict(storage.Values);
            }

            string data = JsonUtility.ToJson(_accounts[accountId]);

            if (string.IsNullOrEmpty(data))
            {
                OwlLogger.LogError($"Converting Account {accountId} to Json failed!", GameComponent.Persistence);
                return -2;
            }

            try
            {
                File.WriteAllText(pathToFile, data);
            }
            catch(Exception e)
            {
                OwlLogger.LogError($"Exception while Persisting account {accountId}: {e.Message}", GameComponent.Persistence);
                return -3;
            }
            
            return 0;
        }

        private string MakeFilePathForAccount(string accountId)
        {
            return Path.Combine(_folderPath, $"{accountId}.accdb");
        }

        public override int DeleteAccount(string accountId)
        {
            if(!DoesAccountExist(accountId))
            {
                OwlLogger.LogError($"Can't delete account {accountId} - doesn't exist.", GameComponent.Persistence);
                return -1;
            }

            // don't delete characters here - no access to CharDatabase (circular dependency).
            // Let the Dummyserver clean that up

            _accounts.Remove(accountId);

            string path = MakeFilePathForAccount(accountId);
            try
            {
                File.Delete(path);
            }
            catch(Exception e)
            {
                OwlLogger.LogError($"Exception while deleting account file {path}: {e.Message}", GameComponent.Persistence);
                return -2;
            }

            return 0;
        }

        public override bool GetConfigValue(string accountId, int configKey, out int value)
        {
            if(!_storedAccConfigs.TryGetValue(accountId, out RemoteConfigStorage storage))
            {
                value = 0;
                return false;
            }

            if(!storage.TryGetConfigValue(configKey, out int result))
            {
                value = 0;
                return false;
            }
            value = result;
            return true;
        }

        public override void SetConfigValue(string accountId, int configKey, int value)
        {
            if (!_storedAccConfigs.TryGetValue(accountId, out RemoteConfigStorage storage))
            {
                storage = new(null);
                _storedAccConfigs.Add(accountId, storage);
            }

            storage.SetConfigValue(configKey, value);
        }
    }
}

