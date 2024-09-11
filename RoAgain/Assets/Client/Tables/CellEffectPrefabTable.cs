using OwlLogging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    [Serializable]
    public class CellEffectPrefabData
    {
        public GameObject Prefab;
    }

    [CreateAssetMenu(fileName = "CellEffectTable", menuName = "ScriptableObjects/CellEffectTable")]
    public class CellEffectPrefabTable : GenericTable<CellEffectType, CellEffectPrefabData, CellEffectPrefabTable> { }
}