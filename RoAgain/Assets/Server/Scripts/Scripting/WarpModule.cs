using OwlLogging;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Server
{
    public class WarpDefinition
    {
        public string SourceMapId;
        public Coordinate BoundsMin;
        public Coordinate BoundsMax;
        public MapCoordinate TargetMapCoord;
        // Optional Parameters here
    }

    public class WarpModule
    {
        private Dictionary<string, List<WarpDefinition>> _defsByMapId = new();

        private WarpLoader _warpLoader = new(); // May need more than one object of this for script versioning or parallelisation

        public int Initialize()
        {
            return 0;
        }

        public void LoadDefinitions()
        {
            List<WarpDefinition> warpDefs = new();
            List<string> filePaths = GetWarpFilePaths();
            foreach (string path in filePaths)
            {
                List<WarpDefinition> fileDefs = _warpLoader.ParseFile(path);
                if (fileDefs != null)
                    warpDefs.AddRange(fileDefs);
            }

            foreach (WarpDefinition def in warpDefs)
            {
                if (!_defsByMapId.ContainsKey(def.SourceMapId))
                    _defsByMapId[def.SourceMapId] = new();

                _defsByMapId[def.SourceMapId].Add(def);
            }

            ValidateNpcDefinitions();
        }

        public void ValidateNpcDefinitions()
        {
            HashSet<int> usedNpcIds = new();
            int passCount = 0;
            int failCount = 0;
            List<int> faultyDefIdxs = new();
            foreach (List<WarpDefinition> defList in _defsByMapId.Values)
            {
                faultyDefIdxs.Clear();
                for (int i = 0; i < defList.Count; i++)
                {
                    WarpDefinition def = defList[i];

                    if (string.IsNullOrWhiteSpace(def.SourceMapId))
                    {
                        OwlLogger.LogError($"Warp at {def.SourceMapId}/{def.BoundsMin}/{def.BoundsMax} doesn't have Source-Map set!", GameComponent.Scripts);
                        faultyDefIdxs.Add(i);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(def.TargetMapCoord.MapId))
                    {
                        OwlLogger.LogError($"Warp at {def.SourceMapId}/{def.BoundsMin}/{def.BoundsMax} doesn't have Target-Map set!", GameComponent.Scripts);
                        faultyDefIdxs.Add(i);
                        continue;
                    }

                    if (def.BoundsMin == Coordinate.INVALID || def.BoundsMax == Coordinate.INVALID)
                    {
                        OwlLogger.LogError($"Warp at {def.SourceMapId}/{def.BoundsMin}/{def.BoundsMax} has invalid bounds!", GameComponent.Scripts);
                        faultyDefIdxs.Add(i);
                        continue;
                    }

                    if(def.TargetMapCoord.Coord == Coordinate.INVALID)
                    {
                        OwlLogger.LogError($"Warp at {def.SourceMapId}/{def.BoundsMin}/{def.BoundsMax} has invalid target coords!", GameComponent.Scripts);
                        faultyDefIdxs.Add(i);
                        continue;
                    }

                    if (def.SourceMapId == def.TargetMapCoord.MapId)
                    {
                        // does the warp loop to itself?
                        if(def.BoundsMin.X <= def.TargetMapCoord.Coord.X && def.TargetMapCoord.Coord.X <= def.BoundsMax.X
                        && def.BoundsMin.Y <= def.TargetMapCoord.Coord.Y && def.TargetMapCoord.Coord.Y <= def.BoundsMax.Y)
                        {
                            OwlLogger.LogError($"Warp at {def.SourceMapId}/{def.BoundsMin}/{def.BoundsMax} loops onto itself!", GameComponent.Scripts);
                            faultyDefIdxs.Add(i);
                            continue;
                        }
                    }

                    passCount++;
                }
                foreach (int faultyIdx in faultyDefIdxs)
                {
                    defList.RemoveAt(faultyIdx);
                    failCount++;
                }
            }

            OwlLogger.LogF("WarpDefinition validation complete: {0} pass, {1} fail", passCount, failCount, GameComponent.Scripts);
        }

        private List<string> GetWarpFilePaths()
        {
            string basePath = Path.Combine(Application.dataPath, "Server", Configuration.Instance.GetMainConfig(ConfigurationKey.WarpDefinitionDirectory));

            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            return new List<string>(Directory.GetFiles(basePath, "*.warp", SearchOption.AllDirectories));
        }

        public List<CellEffectGroup> CreateWarpsForMap(string mapId)
        {
            if (string.IsNullOrWhiteSpace(mapId))
            {
                OwlLogger.LogError("Cant place warps on empty mapId!", GameComponent.Scripts);
                return null;
            }

            if (!_defsByMapId.ContainsKey(mapId))
            {
                return new();
            }

            ServerMapInstance map = AServer.Instance.MapModule.GetMapInstance(mapId);
            if (map == null)
            {
                OwlLogger.LogError($"Can't place warps on map {mapId} - map instance not found!", GameComponent.Scripts);
                return null;
            }

            List<CellEffectGroup> placedWarps = new();
            foreach (WarpDefinition warpDef in _defsByMapId[mapId])
            {
                WarpCellEffectGroup warp = CreateWarp(warpDef);
                RectangleBoundsGridShape shape = new() { IncludeVoid = false, SourceBoundsMin = warpDef.BoundsMin.ToVector(), SourceBoundsMax = warpDef.BoundsMax.ToVector() };
                warp.Create(map.Grid, shape, warpDef.TargetMapCoord.MapId, warpDef.TargetMapCoord.Coord.ToVector());
                placedWarps.Add(warp);
            }

            return placedWarps;
        }

        private WarpCellEffectGroup CreateWarp(WarpDefinition npcDef)
        {
            return new();
        }

        public void Shutdown()
        {
            _defsByMapId.Clear();
            // TODO: Clean up created Warps, just in case the Grid itself doesn't get discarded
        }
    }
}

