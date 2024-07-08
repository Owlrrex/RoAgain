using OwlLogging;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Server
{
    public class NpcModule
    {
        private Dictionary<string, List<NpcDefinition>> _npcDefsByMapId = new();

        private NpcLoader _npcLoader = new(); // May need more than one object of this for script versioning or parallelisation

        public int Initialize()
        {
            return 0;
        }

        public void LoadNpcDefinitions()
        {
            List<NpcDefinition> npcDefs = new();
            List<string> filePaths = GetNpcFilePaths(); 
            foreach(string path in filePaths)
            {
                List<NpcDefinition> fileDefs = _npcLoader.ParseFile(path);
                if(fileDefs != null)
                    npcDefs.AddRange(fileDefs);
            }

            foreach (NpcDefinition def in npcDefs)
            {
                if (!_npcDefsByMapId.ContainsKey(def.MapId))
                    _npcDefsByMapId[def.MapId] = new();

                _npcDefsByMapId[def.MapId].Add(def);
            }

            ValidateNpcDefinitions();
        }

        public bool ValidateNpcDefinitions()
        {
            bool pass = true;
            HashSet<int> usedNpcIds = new();
            int passCount = 0;
            int failCount = 0;
            bool npcPass = true;
            List<int> faultyDefIdxs = new();
            foreach (List<NpcDefinition> defList in _npcDefsByMapId.Values)
            {
                faultyDefIdxs.Clear();
                for(int i = 0; i < defList.Count; i++)
                {
                    NpcDefinition def = defList[i];
                    npcPass = true;
                    if (!usedNpcIds.Add(def.NpcId))
                    {
                        OwlLogger.LogError($"NpcId {def.NpcId} is used more than once!", GameComponent.Scripts);
                        pass = false;
                        npcPass = false;
                        faultyDefIdxs.Add(i);
                    }

                    if (npcPass)
                        passCount++;
                }
                foreach(int faultyIdx in faultyDefIdxs)
                {
                    defList.RemoveAt(faultyIdx);
                    failCount++;
                }
            }

            OwlLogger.LogF("NpcDefinition validation complete: {0} pass, {1} fail", passCount, failCount, GameComponent.Scripts);
            return pass;
        }

        private List<string> GetNpcFilePaths()
        {
            string basePath = Path.Combine(Application.dataPath, "Server", Configuration.Instance.GetMainConfig(ConfigurationKey.NpcDefinitionDirectory));

            if(!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            return new List<string>(Directory.GetFiles(basePath, "*.npc", SearchOption.AllDirectories));
        }

        public List<GridEntity> CreateNpcsForMap(string mapId)
        {
            //  Find correct MapModule & create NPC (don't link to the Script yet, scripts may be lazy-loaded!)
            return null;
        }

        public void Shutdown()
        {
            _npcDefsByMapId.Clear();
        }
    }

    // TODO: Separate file for this, if it will contain also non-npc script commands (like for item scripts)?
    public static class ScriptCommandStrings
    {
        public const string NpcScriptVersion = "sver";
        public const string KeywordNpcHeader = "npc";
    }
}

