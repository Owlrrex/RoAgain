using OwlLogging;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Shared;

namespace Server
{
    public class NpcDefinition
    {
        public int NpcId;
        public MapCoordinate Location;
        public int ModelId;
        public LocalizedStringId NameLocId;
        public int ScriptId;
    }

    public class NpcModule
    {
        private Dictionary<string, List<NpcDefinition>> _npcDefsByMapId = new();

        private NpcLoader _npcLoader = new(); // May need more than one object of this for script versioning or parallelisation

        public int Initialize()
        {
            return 0;
        }

        public void LoadDefinitions()
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
                if (!_npcDefsByMapId.ContainsKey(def.Location.MapId))
                    _npcDefsByMapId[def.Location.MapId] = new();

                _npcDefsByMapId[def.Location.MapId].Add(def);
            }

            ValidateNpcDefinitions();
        }

        public void ValidateNpcDefinitions()
        {
            HashSet<int> usedNpcIds = new();
            int passCount = 0;
            int failCount = 0;
            List<int> faultyDefIdxs = new();
            foreach (List<NpcDefinition> defList in _npcDefsByMapId.Values)
            {
                faultyDefIdxs.Clear();
                for(int i = 0; i < defList.Count; i++)
                {
                    NpcDefinition def = defList[i];
                    if (!usedNpcIds.Add(def.NpcId))
                    {
                        OwlLogger.LogError($"NpcId {def.NpcId} is used more than once!", GameComponent.Scripts);
                        faultyDefIdxs.Add(i);
                        continue;
                    }

                    passCount++;
                }
                foreach(int faultyIdx in faultyDefIdxs)
                {
                    defList.RemoveAt(faultyIdx);
                    failCount++;
                }
            }

            OwlLogger.LogF("NpcDefinition validation complete: {0} pass, {1} fail", passCount, failCount, GameComponent.Scripts);
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
            if (string.IsNullOrWhiteSpace(mapId))
            {
                OwlLogger.LogError("Cant place npcs on empty mapId!", GameComponent.Scripts);
                return null;
            }

            if (!_npcDefsByMapId.ContainsKey(mapId))
            {
                return new();
            }

            MapInstance map = AServer.Instance.MapModule.GetMapInstance(mapId);
            if (map == null)
            {
                OwlLogger.LogError($"Can't place npcs on map {mapId} - map instance not found!", GameComponent.Scripts);
                return null;
            }

            List<GridEntity> placedNpcs = new();
            foreach (NpcDefinition npcDef in _npcDefsByMapId[mapId])
            {
                GridEntity npc = CreateNpc(npcDef);
                map.Grid.PlaceOccupant(npc, npcDef.Location.Coord.ToVector());
                placedNpcs.Add(npc);
            }
            
            // don't link to the Script yet, scripts may be lazy-loaded!
            return placedNpcs;
        }

        private GridEntity CreateNpc(NpcDefinition npcDef)
        {
            return new(npcDef.Location.Coord, npcDef.NameLocId, npcDef.ModelId, 1);
        }

        public void Shutdown()
        {
            _npcDefsByMapId.Clear();
            // TODO: Clean up created NPCs, just in case the Grid itself doesn't get discarded
        }
    }
}

