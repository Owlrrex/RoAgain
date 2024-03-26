using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;

namespace Server
{
    [Serializable]
    public class SkillStaticDataEntry
    {
        public SkillId SkillId;
        public int[] SpCost;
        public float[] BaseCastTime;
        public int[] Range;
        public float[] AnimCd;
        public int[] Var1, Var2, Var3, Var4, Var5;
        public bool[] CanBeInterrupted; // No precedent for this value to change with skilllevel - but it could
        public bool[] IsCastTimeFixed; // No precedent for this value to change with skillevel - but it could

        public T GetValueForLevel<T>(IList<T> array, int level)
        {
            if (level <= 0)
                throw new ArgumentOutOfRangeException("skillLevel");

            // If I have any kind of logic to allow for "skipping" values, it would be decoded here

            if (array == null || array.Count == 0)
                return default;

            if (level >= array.Count)
                return array[^1];
            else return array[level-1];
        }
    }

    [Serializable]
    public class SkillStaticDataList
    {
        public SkillStaticDataEntry[] Entries;
    }

    public class SkillStaticDataDatabase
    {
        private const string FILE_KEY = CachedFileAccess.SERVER_DB_PREFIX + "SkillStaticData";

        private static SkillStaticDataDatabase _instance;

        private Dictionary<SkillId, SkillStaticDataEntry> _data;

        public void Register()
        {
            if (_instance != null)
            {
                OwlLogger.LogError("Duplicate SkillStaticDataDatabase!", GameComponent.Other);
                return;
            }

            SkillStaticDataList dataList = CachedFileAccess.GetOrLoad<SkillStaticDataList>(FILE_KEY, true);
            CachedFileAccess.Purge(FILE_KEY); // Purge to free memory - this db is likely pretty large.

            if (_data == null)
            {
                _data = new();
            }

            foreach (SkillStaticDataEntry entry in dataList.Entries)
            {
                _data.Add(entry.SkillId, entry);
            }

            _instance = this;
        }

        public static void Reload()
        {
            if(_instance == null)
            {
                OwlLogger.LogError("Can't reload SkillStaticData while instance is null!", GameComponent.Other);
                return;
            }

            int result = CachedFileAccess.Load<SkillStaticDataList>(FILE_KEY, true);
            if(result != 0)
            {
                OwlLogger.LogError("Loading SkillStaticData failed!", GameComponent.Other);
                return;
            }

            SkillStaticDataList dataList = CachedFileAccess.Get<SkillStaticDataList>(FILE_KEY);
            CachedFileAccess.Purge(FILE_KEY);

            _instance._data.Clear();
            foreach (SkillStaticDataEntry entry in dataList.Entries)
            {
                _instance._data.Add(entry.SkillId, entry);
            }
        }

        public static SkillStaticDataEntry GetSkillStaticData(SkillId skillId)
        {
            if (_instance == null)
            {
                OwlLogger.LogError("Tried to get SkillStaticData before Database was available", GameComponent.Other);
                return null;
            }

            if(!_instance._data.ContainsKey(skillId))
            {
                OwlLogger.LogError($"Tried to get SkillStaticData for Skill {skillId} but none was found!", GameComponent.Other);
                return null;
            }

            return _instance._data[skillId];
        }
    }
}

