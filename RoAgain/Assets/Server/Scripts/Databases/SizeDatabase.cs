using OwlLogging;
using System.Collections.Generic;
using System;
using Shared;

namespace Server
{
    public abstract class ASizeDatabase
    {
        protected static ASizeDatabase _instance;

        protected Dictionary<EquipmentType, (float, float, float)> _1handData;
        protected Dictionary<EquipmentType, (float, float, float)> _2handData;

        public abstract void Register();

        public static float GetMultiplierForWeaponAndSize(EquipmentType weaponType, EntitySize size, bool isTwoHanded)
        {
            if (_instance == null || _instance._1handData == null || _instance._2handData == null)
            {
                OwlLogger.LogError("Tried to get Multiplier for Weapon & Size before SizeDatabase was available", GameComponent.Other);
                return -1.0f;
            }

            var data = isTwoHanded ? _instance._2handData : _instance._1handData;
            switch (size)
            {
                case EntitySize.Small:
                    return data[weaponType].Item1;
                case EntitySize.Medium:
                    return data[weaponType].Item2;
                case EntitySize.Large:
                    return data[weaponType].Item3;
            }

            OwlLogger.LogError($"Can't find size modifier for size {size}", GameComponent.Other);
            return -2.0f;
        }
    }

    public class SizeDatabase : ASizeDatabase
    {
        private class SizeDatabaseData
        {
            public List<SizeDatabaseEntry> OneHndData = new();
            public List<SizeDatabaseEntry> TwoHndData = new();
        }

        [Serializable]
        private  class SizeDatabaseEntry
        {
            public EquipmentType WeaponType; // TODO: Transfer this to a non-Scriptable Object db so we can use a ulong-based enum
            public float Small;
            public float Medium;
            public float Large;
        }

        private const string FILE_KEY = CachedFileAccess.SERVER_DB_PREFIX + "SizeDatabase";

        public override void Register()
        {
            if (_instance != null)
            {
                OwlLogger.LogError("Duplicate SizeDatabase!", GameComponent.Other);
                return;
            }

            SizeDatabaseData persData = CachedFileAccess.GetOrLoad<SizeDatabaseData>(FILE_KEY, true);
            if (persData == null)
            {
                OwlLogger.LogError("SizeDatabase failed to register - null data!", GameComponent.Other);
                return;
            }

            _1handData = new();
            foreach (SizeDatabaseEntry entry in persData.OneHndData)
            {
                _1handData.Add(entry.WeaponType, (entry.Small, entry.Medium, entry.Large));
            }
            _2handData = new();
            foreach (SizeDatabaseEntry entry in persData.TwoHndData)
            {
                _2handData.Add(entry.WeaponType, (entry.Small, entry.Medium, entry.Large));
            }
            _instance = this;
        }
    }
}