using OwlLogging;
using Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    [Serializable]
    public class JobTableData
    {
        public GameObject ModelPrefab;
        // TODO: Prefabs & other data
    }

    [CreateAssetMenu(fileName = "JobTable", menuName = "ScriptableObjects/JobTable")]
    public class JobTable : GenericTable<JobId, JobTableData, JobTable> { }
}