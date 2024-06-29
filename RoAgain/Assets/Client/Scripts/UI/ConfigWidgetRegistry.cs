using OwlLogging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    [CreateAssetMenu(fileName = "ConfigWidgetRegistry", menuName = "ScriptableObjects/ConfigWidgetRegistry")]
    public class ConfigWidgetRegistry : ScriptableObject
    {
        private static ConfigWidgetRegistry _instance;

        [SerializeField]
        private List<GameObject> _prefabs;

        public void Init()
        {
            if (_instance != null)
            {
                OwlLogger.LogError("Duplicate ConfigWidgetRegistry initialization currently not supported!", GameComponent.UI);
                return;
            }

            bool anyBroken = false;
            foreach (GameObject go in _prefabs)
            {
                if (go.GetComponentInChildren<AConfigLineWidget>() == null)
                {
                    OwlLogger.LogError($"ConfigWidgetRegistry {name} contains invalid ConfigLineWidget prefab {go.name}!", GameComponent.UI);
                    anyBroken = true;
                }
            }

            if (anyBroken)
                return;

            _instance = this;
        }

        public static GameObject GetPrefabForIndex(int index)
        {
            if(_instance == null)
            {
                OwlLogger.LogError("Tried to query ConfigWidgetRegistry before any was registered!", GameComponent.UI);
                return null;
            }

            if(index < 0 || index >= _instance._prefabs.Count)
            {
                OwlLogger.LogError($"Invalid index for ConfigWidget: {index}", GameComponent.UI);
                return null;
            }
            return _instance._prefabs[index];
        }
    }
}
