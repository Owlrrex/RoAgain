using OwlLogging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Server
{
    [CreateAssetMenu(fileName = "ElementsDatabase", menuName = "ScriptableObjects/ElementsDatabase")]
    public class ElementsDatabase : ScriptableObject
    {
        private static ElementsDatabase _instance;

        [Serializable]
        private class ElementsDatabaseEntry
        {
#pragma warning disable CS0649 // Field 'ElementsDatabase.ElementsDatabaseEntry.OffensiveElement' is never assigned to, and will always have its default value 
            public EntityElement OffensiveElement;
#pragma warning restore CS0649 // Field 'ElementsDatabase.ElementsDatabaseEntry.OffensiveElement' is never assigned to, and will always have its default value 
            public List<ElementsDatabaseSubEntry> DefensiveElements;
        }

        [Serializable]
        private class ElementsDatabaseSubEntry
        {
            public EntityElement DefensiveElement;
            public float Multiplier;
        }

        [SerializeField]
        private List<ElementsDatabaseEntry> _entries;

        private Dictionary<EntityElement, Dictionary<EntityElement, float>> _data;

        public void Register()
        {
            if (_entries == null)
            {
                OwlLogger.LogError($"Can't register ElementsDatabase with null entries!", GameComponent.Other);
                return;
            }

            if (_instance != null)
            {
                OwlLogger.LogError("Duplicate ElementsDatabase!", GameComponent.Other);
                return;
            }

            if (_data == null)
            {
                _data = new();
                foreach (ElementsDatabaseEntry entry in _entries)
                {
                    Dictionary<EntityElement, float> defensiveEntries = new();
                    foreach (ElementsDatabaseSubEntry subEntry in entry.DefensiveElements)
                    {
                        defensiveEntries.Add(subEntry.DefensiveElement, subEntry.Multiplier);
                    }
                    _data.Add(entry.OffensiveElement, defensiveEntries);
                }
            }

            _instance = this;
        }

        public static float GetMultiplierForElements(EntityElement offensiveElement, EntityElement defensiveElement)
        {
            if (_instance == null)
            {
                OwlLogger.LogError("Tried to get Multiplier for Elements before ElementsDatabase was available", GameComponent.Other);
                return -10.0f;
            }

            return _instance._data[offensiveElement][defensiveElement];
        }
    }
}