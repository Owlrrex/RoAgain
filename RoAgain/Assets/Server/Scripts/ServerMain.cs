using OwlLogging;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Server
{
    public class ServerMain : MonoBehaviour
    {
        public AServer Server;

        [SerializeField]
        private Rect _titlePlacement;

        [SerializeField]
        private SpawnDatabase _spawnDatabase;

        private MobDatabase _mobDatabase;

        [SerializeField]
        private ElementsDatabase _elementsDatabase;

        [SerializeField]
        private SizeDatabase _sizeDatabase;

        [SerializeField]
        private SkillTreeDatabase _skillTreeDatabase;

        void Start()
        {
            if (Server != null)
            {
                OwlLogger.LogWarning("Tried to double-initialize Server - aborting.", GameComponent.Other);
                return;
            }
            CoreServer serverInstance = new();
            serverInstance.Initialize();
            Server = serverInstance;

            if(!OwlLogger.PrefabNullCheckAndLog(_spawnDatabase, "spawnDatabase", this, GameComponent.Other))
                _spawnDatabase.Register();

            _mobDatabase = new();
            _mobDatabase.Register();

            if(!OwlLogger.PrefabNullCheckAndLog(_elementsDatabase, "elementsDatabase", this, GameComponent.Other))
                _elementsDatabase.Register();

            if(!OwlLogger.PrefabNullCheckAndLog(_sizeDatabase, "sizeDatabase", this, GameComponent.Other))
                _sizeDatabase.Register();

            if(!OwlLogger.PrefabNullCheckAndLog(_skillTreeDatabase, "skillTreeDatabase", this, GameComponent.Other))
                _skillTreeDatabase.Register();

            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            Server?.Update(Time.deltaTime);
        }

        private void OnDestroy()
        {
            Server?.Shutdown();
        }

        private void OnGUI()
        {
            GUI.Label(_titlePlacement, "Ragnarok Again (Server)");
        }
    }
}

