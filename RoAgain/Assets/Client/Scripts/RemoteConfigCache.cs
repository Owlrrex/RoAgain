using OwlLogging;
using System.Collections.Generic;
using Shared;

namespace Client
{
    public class RemoteConfigCache
    {
        private Dictionary<ConfigKey, int> _characterConfig = new();
        private Dictionary<ConfigKey, int> _accountConfig = new();

        private HashSet<ConfigKey> _pendingCharRequests = new();
        private HashSet<ConfigKey> _pendingAccRequests = new();

        private ServerConnection _connection;

        public int Initialize(ServerConnection connection)
        {
            if(connection == null)
            {
                OwlLogger.LogError("Can't initialize RemoteConfigCache with null connection!", GameComponent.Config);
                return -1;
            }

            _connection = connection;
            _connection.ConfigValueReceived += OnConfigValueReceived;

            return 0;
        }

        public void Shutdown()
        {
            ClearAllConfig();

            if(_connection != null)
            {
                _connection.ConfigValueReceived -= OnConfigValueReceived;
                _connection = null;
            }
        }

        private void OnConfigValueReceived(ConfigKey configKey, bool exists, int configValue, bool isAccountStorage)
        {
            OwlLogger.Log($"Received Remote config value: {configKey} = {configValue} (Exists = {exists}, Accountwide = {isAccountStorage})", GameComponent.Other);
            if (isAccountStorage)
            {
                if(exists)
                    AddAccountConfigValue(configKey, configValue);
                _pendingAccRequests.Remove(configKey);
            }
            else
            {
                if (exists)
                    AddCharConfigValue(configKey, configValue);
                _pendingCharRequests.Remove(configKey);
            }
        }

        public void ClearCharacterConfig()
        {
            _characterConfig.Clear();
        }

        public void ClearAllConfig()
        {
            ClearCharacterConfig();
            _accountConfig.Clear();
        }

        public void FetchConfigValue(ConfigKey key, bool useAccountStorage)
        {
            _connection.Send(new ConfigReadRequestPacket() { Key = (int)key, UseAccountStorage = useAccountStorage });
            if (useAccountStorage)
                _pendingAccRequests.Add(key);
            else
                _pendingCharRequests.Add(key);
        }

        public bool AnyRequestsPending()
        {
            return _pendingAccRequests.Count > 0 || _pendingCharRequests.Count > 0;
        }

        public void AddCharConfigValue(ConfigKey key, int value)
        {
            if (key == ConfigKey.Unknown)
            {
                OwlLogger.LogError("Can't use Unknown Configkey in RemoteConfigCache!", GameComponent.Config);
                return;
            }

            _characterConfig[key] = value;
        }

        public void AddAccountConfigValue(ConfigKey key, int value)
        {
            if (key == ConfigKey.Unknown)
            {
                OwlLogger.LogError("Can't use Unknown Configkey in RemoteConfigCache!", GameComponent.Config);
                return;
            }

            _accountConfig[key] = value;
        }

        public void SaveCharConfigValue(ConfigKey key, int value)
        {
            if (key == ConfigKey.Unknown)
            {
                OwlLogger.LogError("Can't use Unknown Configkey in RemoteConfigCache!", GameComponent.Config);
                return;
            }

            AddCharConfigValue(key, value);
            ClientMain.Instance.ConnectionToServer.Send(new ConfigStorageRequestPacket() { Key = (int)key, Value = value, UseAccountStorage = false });
        }

        public void SaveAccountConfigValue(ConfigKey key, int value)
        {
            if (key == ConfigKey.Unknown)
            {
                OwlLogger.LogError("Can't use Unknown Configkey in RemoteConfigCache!", GameComponent.Config);
                return;
            }

            AddAccountConfigValue(key, value);
            ClientMain.Instance.ConnectionToServer.Send(new ConfigStorageRequestPacket() { Key = (int)key, Value = value, UseAccountStorage = true });
        }

        public void ClearCharConfigValue(ConfigKey key)
        {
            if (key == ConfigKey.Unknown)
            {
                OwlLogger.LogError("Can't use Unknown Configkey in RemoteConfigCache!", GameComponent.Config);
                return;
            }

            _characterConfig.Remove(key);
            ClientMain.Instance.ConnectionToServer.Send(new ConfigStorageRequestPacket() { Key = (int)key, Value = ConfigStorageRequestPacket.VALUE_CLEAR, UseAccountStorage = false });
        }

        public void ClearAccountConfigValue(ConfigKey key)
        {
            if (key == ConfigKey.Unknown)
            {
                OwlLogger.LogError("Can't use Unknown Configkey in RemoteConfigCache!", GameComponent.Config);
                return;
            }

            _accountConfig.Remove(key);
            ClientMain.Instance.ConnectionToServer.Send(new ConfigStorageRequestPacket() { Key = (int)key, Value = ConfigStorageRequestPacket.VALUE_CLEAR, UseAccountStorage = true });
        }

        public bool TryGetConfigValueFallthrough(ConfigKey key, out int value)
        {
            if (!TryGetCharConfigValue(key, out value))
            {
                return TryGetAccConfigValue(key, out value);
            }

            return true;
        }

        public bool TryGetCharConfigValue(ConfigKey key, out int value)
        {
            value = 0;
            if (!_characterConfig.ContainsKey(key))
                return false;

            value = _characterConfig[key];
            return true;
        }

        public bool TryGetAccConfigValue(ConfigKey key, out int value)
        {
            value = 0;
            if (!_accountConfig.ContainsKey(key))
                return false;

            value = _accountConfig[key];
            return true;
        }
    }
}

