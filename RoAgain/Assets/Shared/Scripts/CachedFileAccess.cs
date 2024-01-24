using OwlLogging;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Shared
{
    public enum FileAccessDirectory
    {
        Unknown,
        Config,
        Database
    }

    public static class CachedFileAccess
    {
        public const string CONFIG_PREFIX = "CFG_";
        public const string SERVER_DB_PREFIX = "SDB_";
        public const string CLIENT_DB_PREFIX = "CDB_";

        private static Dictionary<string, object> _loadedSets = new();

        public static int Load<T>(string key, bool createIfNotFound) where T : class, new()
        {
            if(string.IsNullOrWhiteSpace(key))
            {
                OwlLogger.LogError("Can't perform CachedFileAccess for empty key!", GameComponent.Other);
                return -3;
            }

            string path = MakePath(key);
            T file;
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    file = JsonUtility.FromJson<T>(json);
                }
                catch(Exception e)
                {
                    OwlLogger.LogError($"Can't read save file for data {typeof(T).Name}: {e.Message}", GameComponent.Config);
                    return -2;
                }
            }
            else
            {
                if(createIfNotFound)
                {
                    // No savefile available: Create default config & write it to disk
                    file = new();
                    Save(key, file);
                }
                else
                {
                    return -1;
                }
            }

            if(file == null)
            {
                OwlLogger.LogError($"Loading save file for data {typeof(T).Name} returned null object!", GameComponent.Other);
                return -3;
            }

            _loadedSets[key] = file;
            return 0;
        }

        public static T Get<T>(string key) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                OwlLogger.LogError("Can't perform CachedFileAccess for empty key!", GameComponent.Other);
                return null;
            }

            if (!IsLoaded(key))
            {
                OwlLogger.LogError($"Can't get configuration of type {typeof(T).Name} - not yet loaded!", GameComponent.Config);
                return null;
            }

            return _loadedSets[key] as T;
        }

        public static T GetOrLoad<T>(string key, bool createIfNotFound) where T : class, new()
        {
            if (!IsLoaded(key))
            {
                int loadResult = Load<T>(key, createIfNotFound);
                if(loadResult != 0)
                {
                    OwlLogger.LogError($"Load failed for GetOrLoad of config {key}", GameComponent.Config);
                    return null;
                }
            }

            return Get<T>(key);
        }

        public static bool IsLoaded(string key)
        {
            return _loadedSets.ContainsKey(key);
        }

        public static int Save<T>(string key, T newData) where T : class, new()
        {
            string path = MakePath(key);
            try
            {
                string folderPath = Path.GetDirectoryName(path);
                Directory.CreateDirectory(folderPath);
            }
            catch (Exception e)
            {
                OwlLogger.LogError($"Failed to create savefile folder for data {typeof(T).Name}, key {key}: {e.Message}", GameComponent.Config);
                return -1;
            }

            string json = JsonUtility.ToJson(newData);
            try
            {
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                OwlLogger.LogError($"Failed to write savedata to file for data {typeof(T).Name}, key {key}: {e.Message}", GameComponent.Config);
                return -2;
            }

            _loadedSets[key] = newData;
            return 0;
        }
        
        private static string MakePath(string key)
        {
            Debug.Log("persistentDataPath: " + Application.persistentDataPath);
            Debug.Log("consoleLogPath: " + Application.consoleLogPath);
            Debug.Log("dataPath: " + Application.dataPath);
            Debug.Log("streamingAssetsPath: " + Application.streamingAssetsPath);
            Debug.Log("temporaryCachePath: " + Application.temporaryCachePath);

            if (key.StartsWith(CONFIG_PREFIX))
            {
                return Path.Combine(Application.persistentDataPath, "Config", key.Remove(0, CONFIG_PREFIX.Length) + ".cfg");
            }
            else if(key.StartsWith(SERVER_DB_PREFIX))
            {
                return Path.Combine(Application.dataPath, "Server", "Databases", key.Remove(0, SERVER_DB_PREFIX.Length) + ".db");
            }
            else if(key.StartsWith(CLIENT_DB_PREFIX))
            {
                return Path.Combine(Application.dataPath, "Client", "Databases", key.Remove(0, CLIENT_DB_PREFIX.Length) + ".db");
            }
            return "";
        }

        public static int Purge(string key)
        {
            if (!_loadedSets.ContainsKey(key))
            {
                OwlLogger.LogWarning($"Tried to purge BufferedFileAccess for type {key} that wasn't loaded yet!", GameComponent.Other);
                return -1;
            }

            _loadedSets.Remove(key);
            return 0;
        }
    }
}

