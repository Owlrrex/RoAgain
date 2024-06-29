using System;
using UnityEngine;

namespace Client
{
    public abstract class AConfigLineWidget : MonoBehaviour
    {
        public abstract void Init(ConfigKey key);
        public Action<ConfigKey> ValueChanged;
        public abstract void Save();
    }
}