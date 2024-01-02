using OwlLogging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Server
{
    [Serializable]
    public class MobDataStatic
    {
        public int MobId; // Not sure if needed here?

        public string DefaultDisplayName;
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
        public int SoftDef = -1;
        public float HardMDef;
        public int SoftMDef = -1;
        public int Flee = -1;
        public int Hit = -1;
    }

    [CreateAssetMenu(fileName = "MobDatabase", menuName = "ScriptableObjects/MobDatabase")]
    public class MobDatabase : ScriptableObject
    {
        public static MobDatabase Instance;

        [Serializable]
        private class MobDatabaseEntry
        {
            public int MobId;
            public MobDataStatic Data;
        }

        [SerializeField]
        private MobDatabaseEntry[] _entries;

        // Need that data-type here
        private Dictionary<int, MobDataStatic> _mobdataById;

        public void Register()
        {
            if (_entries == null)
            {
                OwlLogger.LogError($"Can't register MobDatabase with null entries!", GameComponent.Other);
                return;
            }

            if (Instance != null)
            {
                OwlLogger.LogError("Duplicate MobDatabase!", GameComponent.Other);
                return;
            }

            if (_mobdataById == null)
            {
                _mobdataById = new();
                foreach (MobDatabaseEntry entry in _entries)
                {
                    _mobdataById.Add(entry.MobId, entry.Data);
                }
            }

            Instance = this;
        }

        public static MobDataStatic GetMobDataForId(int mobId)
        {
            if (Instance == null)
            {
                OwlLogger.LogError("Tried to get MobData for MobId before MobDatabase was available", GameComponent.Other);
                return null;
            }

            return Instance._mobdataById[mobId];
        }
    }
}