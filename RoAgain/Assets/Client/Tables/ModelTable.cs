using System;
using UnityEngine;

namespace Client
{
    [Serializable]
    public class ModelData
    {
        public GameObject Prefab;
    }

    [CreateAssetMenu(fileName = "ModelTable", menuName = "ScriptableObjects/ModelTable")]
    public class ModelTable : GenericTable<int, ModelData, ModelTable> { }
}

