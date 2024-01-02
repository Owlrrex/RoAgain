using System;
using System.Collections.Generic;

namespace Shared
{
    [Serializable]
    public class DictionarySerializationWrapper<K, V>
    {
        [Serializable]
        public class Entry
        {
            public K key;
            public V value;
        }

        public List<Entry> entries = new();

        public DictionarySerializationWrapper(Dictionary<K, V> dict)
        {
            FromDict(dict);
        }

        public DictionarySerializationWrapper() { }

        public Dictionary<K, V> ToDict()
        {
            Dictionary<K, V> dict = new();
            foreach (Entry entry in entries)
            {
                dict.Add(entry.key, entry.value);
            }
            return dict;
        }

        public void FromDict(Dictionary<K, V> dict)
        {
            entries.Clear();

            if (dict == null)
                return;

            foreach (KeyValuePair<K, V> kvp in dict)
            {
                entries.Add(new() { key = kvp.Key, value = kvp.Value });
            }
        }
    }
}