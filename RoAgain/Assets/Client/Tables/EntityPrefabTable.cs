using System;
using UnityEngine;

namespace Client
{
    public enum EntityType
    {
        Unknown,
        GenericGrid,
        GenericBattle,
        RemoteCharacter,
        LocalCharacter,
    }

    [Serializable]
    public class EntityPrefabData
    {
        public GameObject Prefab;
    }

    [CreateAssetMenu(fileName = "EntityPrefabTable", menuName = "ScriptableObjects/EntityPrefabTable")]
    public class EntityPrefabTable : GenericTable<EntityType, EntityPrefabData, EntityPrefabTable> { }
}