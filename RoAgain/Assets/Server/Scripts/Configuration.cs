using OwlLogging;
using Shared;
using System.Collections.Generic;
using System.IO;
using MainConfigPersistent = Shared.DictionarySerializationWrapper<Server.ConfigurationKey, string>;

namespace Server
{
    public enum ConfigurationKey
    {
        Unknown,
        TestServerConfigEntry,
        ChatCommandSymbol,
        HitFleeEqualChance,
        BattleMultiplicativeStacking,
        NpcDefinitionDirectory,
        WarpDefinitionDirectory,
        NewCharacterSpawn,
        NewCharacterSave,
        NewCharacterStatPoints
    }

    public class Configuration
    {
        private const string CONFIG_FILE_KEY = CachedFileAccess.CONFIG_PREFIX + "ServerConfig";

        public static Configuration Instance { get; private set; }

        private Dictionary<ConfigurationKey, string> _mainConfig = new();

        public int LoadConfig()
        {
            if (Instance != null && Instance != this)
            {
                OwlLogger.LogError("Can't Load a second config object when one already exists - use existing instance!", GameComponent.Config);
                return -1;
            }

            MainConfigPersistent mainPers = CachedFileAccess.GetOrLoad<MainConfigPersistent>(CONFIG_FILE_KEY, true);
            if (mainPers != null) // indicates file didn't exist
            {
                _mainConfig = mainPers.ToDict();
            }

            // Validate Config
            bool changedAnyConfig = FillInDefaultMiscConfig();

            if (changedAnyConfig)
            {
                SaveConfig();
            }

            CachedFileAccess.Purge(CONFIG_FILE_KEY);

            Instance = this;
            return 0;
        }

        public bool FillInDefaultMiscConfig()
        {
            bool anyChange = false;
            anyChange |= _mainConfig.TryAdd(ConfigurationKey.TestServerConfigEntry, "testValue1");
            anyChange |= _mainConfig.TryAdd(ConfigurationKey.ChatCommandSymbol, "#");
            anyChange |= _mainConfig.TryAdd(ConfigurationKey.HitFleeEqualChance, "80");
            anyChange |= _mainConfig.TryAdd(ConfigurationKey.BattleMultiplicativeStacking, "0");
            anyChange |= _mainConfig.TryAdd(ConfigurationKey.NpcDefinitionDirectory, Path.Combine("Server", "Databases", "NpcDefs"));
            anyChange |= _mainConfig.TryAdd(ConfigurationKey.WarpDefinitionDirectory, Path.Combine("Server", "Databases", "WarpDefs"));
            anyChange |= _mainConfig.TryAdd(ConfigurationKey.NewCharacterSpawn, "test_map/5/5");
            anyChange |= _mainConfig.TryAdd(ConfigurationKey.NewCharacterSave, "test_map/5/5");
            anyChange |= _mainConfig.TryAdd(ConfigurationKey.NewCharacterStatPoints, "44");
            // Add additional config entries here

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

        public string GetMainConfig(ConfigurationKey key)
        {
            if (!_mainConfig.ContainsKey(key))
            {
                return null;
            }

            return _mainConfig[key];
        }
    }
}

