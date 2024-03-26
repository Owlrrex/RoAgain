using OwlLogging;
using Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Server
{
    [Serializable]
    public class JobStaticData
    {
        public JobId JobId;

        [Serializable]
        private class JobBonusLevelEntry
        {
            public EntityPropertyType PropertyType;
            public List<int> Levels;
        }

        [SerializeField]
        private List<JobBonusLevelEntry> _entries = new();

        private Dictionary<EntityPropertyType, List<int>> _bonuses = new();

        public float HpValueA;
        public float HpValueB;

        public float SpValue;

        public float WeightValue;

        // TODO: Aspd values

        public void Initialize()
        {
            _bonuses.Clear();

            foreach(var entry in _entries)
            {
                entry.Levels.Sort();
                _bonuses.Add(entry.PropertyType, entry.Levels);
            }

            // Clear entires since this data is never written to disk, so no need to keep the serializable field in memory.
            _entries.Clear();
        }

        public int GetJobBonusAtLevel(EntityPropertyType stat, int level)
        {
            if (!_bonuses.ContainsKey(stat))
            {
                return 0;
            }

            List<int> levels = _bonuses[stat];
            int bonus = 0;
            foreach (int l in levels)
            {
                if(level < l)
                    break;

                bonus++;
            }

            return bonus;
        }
    }

    public class JobDatabase
    {
        [Serializable]
        private class JobStaticDataList
        {
            public List<JobStaticData> Data;
        }

        private const string FILE_KEY = CachedFileAccess.SERVER_DB_PREFIX + "JobDatabase";

        private static JobDatabase _instance;

        private Dictionary<JobId, JobStaticData> _data;

        public int Register()
        {
            if (_instance != null)
            {
                OwlLogger.LogError("Duplicate JobDatabase!", GameComponent.Other);
                return -1;
            }

            JobStaticDataList dataList = CachedFileAccess.GetOrLoad<JobStaticDataList>(FILE_KEY, true);
            CachedFileAccess.Purge(FILE_KEY); // Purge to free memory - this db is likely pretty large.

            if(_data == null)
            {
                _data = new();
            }
            else
            {
                _data.Clear();
            }

            foreach (JobStaticData entry in dataList.Data)
            {
                _data.Add(entry.JobId, entry);
                entry.Initialize();
            }

            _instance = this;
            return 0;
        }

        //public static void Reload()
        //{
        //    // This likely needs ALOT of engineering to make sure all necessary systems have their data updated
        //    // Not sure if it's a good idea to implement this at all
        //}

        public static JobStaticData GetJobData(JobId jobId)
        {
            if (_instance == null)
            {
                OwlLogger.LogError("Tried to get JobStaticData before Database was available", GameComponent.Other);
                return null;
            }

            if (!_instance._data.ContainsKey(jobId))
            {
                OwlLogger.LogError($"Tried to get JobStaticData for Skill {jobId} but none was found!", GameComponent.Other);
                return null;
            }

            return _instance._data[jobId];
        }
    }
}