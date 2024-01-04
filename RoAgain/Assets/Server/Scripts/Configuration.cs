using OwlLogging;
using Shared;
using System.Collections.Generic;

using MainConfigPersistent = Shared.DictionarySerializationWrapper<Server.ConfigurationKey, string>;

namespace Server
{
    public enum ConfigurationKey
    {
        Unknown,
        TestServerConfigEntry
    }

    public class Configuration
    {
        private const string CONFIG_FILE_KEY = "ServerConfig";

        public static Configuration Instance { get; private set; }

        private Dictionary<ConfigurationKey, string> _mainConfig = new();

        public int LoadConfig()
        {
            if (Instance != null && Instance != this)
            {
                OwlLogger.LogError("Can't Load a second config object when one already exists - use existing instance!", GameComponent.Config);
                return -1;
            }

            bool changedAnyConfig = false;

            MainConfigPersistent mainPers = CachedFileAccess.GetOrLoad<MainConfigPersistent>(CONFIG_FILE_KEY, true);
            if (mainPers == null) // indicates file didn't exist
            {
                LoadDefaultMiscConfig();
                changedAnyConfig = true;
            }
            else
            {
                _mainConfig = mainPers.ToDict();
            }

            // Validate Misc Config
            changedAnyConfig |= FillInDefaultMiscConfig();

            if (changedAnyConfig)
            {
                SaveConfig();
            }
            else
            {
                CachedFileAccess.Purge(CONFIG_FILE_KEY);
            }

            Instance = this;
            return 0;
        }

        public int LoadDefaultMiscConfig()
        {
            _mainConfig.Clear();

            _mainConfig.Add(ConfigurationKey.TestServerConfigEntry, "testServerValue1");
            // TODO: Config entries here

            return 0;
        }

        public bool FillInDefaultMiscConfig()
        {
            bool anyChange = false;
            anyChange |= _mainConfig.TryAdd(ConfigurationKey.TestServerConfigEntry, "testValue1");
            // TODO: Config entries here

            return anyChange;
        }

        public int SaveConfig()
        {
            if (_mainConfig == null || _mainConfig.Count == 0)
            {
                return 0;
            }

            MainConfigPersistent miscPers = new(_mainConfig);

            int mainResult = CachedFileAccess.Save(CONFIG_FILE_KEY, miscPers);

            CachedFileAccess.Purge(CONFIG_FILE_KEY);

            if (mainResult != 0)
            {
                OwlLogger.LogError($"Saving of some configurations failed. mainResult = {mainResult}", GameComponent.Other);
                return -1;
            }

            return 0;
        }
    }
}

