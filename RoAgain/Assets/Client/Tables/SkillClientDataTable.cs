using OwlLogging;
using Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    [Serializable]
    public class SkillClientData
    {
        public Sprite Sprite;
        public string Name;
        public string Description;
    }

    [CreateAssetMenu(fileName = "SkillClientDataTable", menuName = "ScriptableObjects/SkillClientData", order = 5)]
    public class SkillClientDataTable : ScriptableObject
    {
        public static SkillClientDataTable Instance;

        [Serializable]
        private class Entry
        {
            public SkillId Type;
            public SkillClientData Data;
        }

        [SerializeField]
        private List<Entry> _entries;

        private Dictionary<SkillId, SkillClientData> _skillDataById;

        public void Register()
        {
            if (_entries == null)
            {
                OwlLogger.LogError($"Can't register SkillClientDataTable with null entries!", GameComponent.Other);
                return;
            }

            if (Instance != null)
            {
                OwlLogger.LogError("Duplicate SkillClientDataTable!", GameComponent.Other);
                return;
            }

            if (_skillDataById == null)
            {
                _skillDataById = new();
                foreach (Entry entry in _entries)
                {
                    _skillDataById.Add(entry.Type, entry.Data);
                }
            }

            Instance = this;
        }

        public static SkillClientData GetDataForId(SkillId skillId)
        {
            if (Instance == null)
            {
                OwlLogger.LogError($"Tried to get Data for SkillId {skillId} before SkillClientDataTable was available", GameComponent.Other);
                return null;
            }

            if (!Instance._skillDataById.ContainsKey(skillId))
            {
                OwlLogger.LogError($"Tried to get Data for SkillId {skillId} that's not found in Database!", GameComponent.Other);
                return null;
            }

            return Instance._skillDataById[skillId];
        }
    }
}
