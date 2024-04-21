using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;

using MobDataPersistent = Shared.DictionarySerializationWrapper<int, Server.MobDataStatic>;

namespace Server
{
    [Serializable]
    public class MobDataStatic
    {
        public LocalizedStringId DefaultDisplayName;
        public int ModelId;

        public int BaseExpReward;
        public int JobExpReward;

        public int BaseLvl;
        public EntityRace Race;
        public EntitySize Size;
        public EntityElement Element;

        public int MaxHp;
        public int MaxSp;
        public int BaseAttackRange = 1;
        public int Str;
        public int Agi;
        public int Vit;
        public int Int;
        public int Dex;
        public int Luk;
        public int MinAtk;
        public int MaxAtk;
        public int MinMatk;
        public int MaxMatk;
        public float HardDef;
        public float HardMDef;
        public float Apm;
        public float Movespeed;

        // TODO: Skills?
        // TODO: AI
        // TODO: Loot
    }

    public class MobDatabase
    {
        private const string FILE_KEY = CachedFileAccess.SERVER_DB_PREFIX + "MobDatabase";

        private static MobDatabase _instance;

        private Dictionary<int, MobDataStatic> _mobdataById;

        public void Register()
        {
            if (_instance != null)
            {
                OwlLogger.LogError("Duplicate MobDatabase!", GameComponent.Other);
                return;
            }

            MobDataPersistent persData = CachedFileAccess.GetOrLoad<MobDataPersistent>(FILE_KEY, true);
            if(persData == null)
            {
                OwlLogger.LogError("MobDatabase failed to register - null data!", GameComponent.Other);
                return;
            }

            _mobdataById = persData.ToDict();
            CachedFileAccess.Purge(FILE_KEY);

            _instance = this;
        }

        public static MobDataStatic GetMobDataForId(int mobTypeId)
        {
            if (_instance == null)
            {
                OwlLogger.LogError($"Tried to get MobData for MobTypeId {mobTypeId} before MobDatabase was available", GameComponent.Other);
                return null;
            }

            if(!_instance._mobdataById.ContainsKey(mobTypeId))
            {
                OwlLogger.LogError($"No MobData found for MobTypeId {mobTypeId}!", GameComponent.Other);
                return null;
            }

            return _instance._mobdataById[mobTypeId];
        }
    }
}