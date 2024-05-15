using System.Collections.Generic;

namespace Client
{
    public class RemoteConfigCache
    {
        private Dictionary<RemoteConfigKey, int> _characterConfig = new();
        private Dictionary<RemoteConfigKey, int> _accountConfig = new();

        public void ClearCharacterConfig()
        {
            _characterConfig.Clear();
        }

        public void ClearAllConfig()
        {
            ClearCharacterConfig();
            _accountConfig.Clear();
        }

        public void AddCharConfigValue(RemoteConfigKey key, int value)
        {
            _characterConfig[key] = value;
        }

        public void AddAccountConfigValue(RemoteConfigKey key, int value)
        {
            _accountConfig[key] = value;
        }

        public int GetConfigValueFallthrough(RemoteConfigKey key)
        {
            if(_accountConfig.ContainsKey(key))
            {
                return GetAccConfigValue(key);
            }
            
            return GetCharConfigValue(key);
        }

        public int GetCharConfigValue(RemoteConfigKey key)
        {
            if (!_characterConfig.ContainsKey(key))
                return 0;
            return _characterConfig[key];
        }

        public int GetAccConfigValue(RemoteConfigKey key)
        {
            if (!_accountConfig.ContainsKey(key))
                return 0;
            return _accountConfig[key];
        }
    }
}

