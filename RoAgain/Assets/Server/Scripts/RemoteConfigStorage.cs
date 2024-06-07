using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Server
{
    /// <summary>
    /// Contains numeric values of server-side-stored config keys that this server understands.
    /// The server can store keys of any value, but these ones can be expected to be used by the server itself
    /// </summary>
    public enum RemoteConfigKey
    {
        Unknown = -1
    }

    public class RemoteConfigStorage
    {
        public Dictionary<int, int> Values { get; private set; } = new();

        public RemoteConfigStorage(Dictionary<int, int> inValues)
        {
            if (inValues == null)
                return;

            foreach (var kvp in inValues)
            {
                Values.Add(kvp.Key, kvp.Value);
            }
        }

        public bool TryGetConfigValue(int key, out int value)
        {
            value = 0;
            if (!Values.ContainsKey(key))
                return false;
            value = Values[key];
            return true;
        }

        public void SetConfigValue(int key, int value)
        {
            Values[key] = value;
        }
    }
}
