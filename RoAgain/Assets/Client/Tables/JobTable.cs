using OwlLogging;
using Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    [Serializable]
    public class JobTableEntry
    {
        public JobId JobId;
        public GameObject ModelPrefab;
        // TODO: Prefabs & other data
    }

    [CreateAssetMenu(fileName = "JobTable", menuName = "ScriptableObjects/JobTable", order = 6)]
    public class JobTable : ScriptableObject
    {
        public static JobTable Instance;

        [SerializeField]
        private List<JobTableEntry> _entries;

        private Dictionary<JobId, JobTableEntry> _dataById;

        public void Register()
        {
            if (_entries == null)
            {
                OwlLogger.LogError($"Can't register MapPrefabTable with null entries!", GameComponent.Other);
                return;
            }

            if (Instance != null)
            {
                OwlLogger.LogError("Duplicate MapPrefabTable!", GameComponent.Other);
                return;
            }

            _dataById ??= new();

            foreach (JobTableEntry entry in _entries)
            {
                _dataById.Add(entry.JobId, entry);
            }

            Instance = this;
        }

        public static JobTableEntry GetDataById(JobId jobId)
        {
            if (Instance == null)
            {
                OwlLogger.LogError($"Tried to get data for jobId {jobId} before JobTable was available", GameComponent.Other);
                return null;
            }

            if(!Instance._dataById.ContainsKey(jobId))
            {
                OwlLogger.LogError($"JobTable doesn't contain entry for jobId {jobId}!", GameComponent.Other);
                return null;
            }

            return Instance._dataById[jobId];
        }
    }
}