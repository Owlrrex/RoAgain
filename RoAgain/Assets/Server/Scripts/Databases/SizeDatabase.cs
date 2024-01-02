using OwlLogging;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Server
{
    [CreateAssetMenu(fileName = "SizeDatabase", menuName = "ScriptableObjects/SizeDatabase")]
    public class SizeDatabase : ScriptableObject
    {
        private static SizeDatabase _instance;

        [Serializable]
        private class SizeDatabaseEntry
        {
            public AttackWeaponType WeaponType;
            public float Small;
            public float Medium;
            public float Large;
        }

        [SerializeField]
        private SizeDatabaseEntry[] _entries;

        private Dictionary<AttackWeaponType, (float, float, float)> _data;

        public void Register()
        {
            if (_entries == null)
            {
                OwlLogger.LogError($"Can't register SizeDatabase with null entries!", GameComponent.Other);
                return;
            }

            if (_instance != null)
            {
                OwlLogger.LogError("Duplicate SizeDatabase!", GameComponent.Other);
                return;
            }

            if (_data == null)
            {
                _data = new();
                foreach (SizeDatabaseEntry entry in _entries)
                {
                    _data.Add(entry.WeaponType, (entry.Small, entry.Medium, entry.Large));
                }
            }

            _instance = this;
        }

        public static float GetMultiplierForWeaponAndSize(AttackWeaponType weaponType, EntitySize size)
        {
            if (_instance == null)
            {
                OwlLogger.LogError("Tried to get Multiplier for Weapon & Size before SizeDatabase was available", GameComponent.Other);
                return -1.0f;
            }

            switch (size)
            {
                case EntitySize.Small:
                    return _instance._data[weaponType].Item1;
                case EntitySize.Medium:
                    return _instance._data[weaponType].Item2;
                case EntitySize.Large:
                    return _instance._data[weaponType].Item3;
            }

            OwlLogger.LogError($"Can't find size modifier for size {size}", GameComponent.Other);
            return -2.0f;
        }
    }
}