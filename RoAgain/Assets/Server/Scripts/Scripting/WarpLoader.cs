using OwlLogging;
using System;
using System.Collections.Generic;
using Shared;

namespace Server
{
    public class WarpLoader
    {
        public List<WarpDefinition> ParseFile(string filePath)
        {
            List<WarpDefinition> npcDefs = new();
            string[] lines = ScriptUtils.LoadRawLinesUncached(filePath);
            if (lines == null || lines.Length == 0)
            {
                return npcDefs;
            }

            string firstLine = lines[0];
            int scriptVersion = ScriptUtils.GetScriptVersion(firstLine);
            if (scriptVersion < 0)
            {
                OwlLogger.LogError($"Script at path {filePath} malformed: Script Version Entry.", GameComponent.Scripts);
                return npcDefs;
            }

            bool ongoingDef = false;
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                if (!lines[i].StartsWith(ScriptKeywords.WarpHeader))
                    continue;

                if (!ongoingDef)
                {
                    WarpDefinition newDef = ParseWarpHeaderLine(lines[i]);
                    if (newDef != null)
                    {
                        OwlLogger.LogF("Npc Definition found for Warp at {0}/{1}/{2}", newDef.SourceMapId, newDef.BoundsMin, newDef.BoundsMax, GameComponent.Scripts);
                        npcDefs.Add(newDef);
                        ongoingDef = true;
                    }
                    else
                    {
                        OwlLogger.LogError($"Malformed WarpHeader in file {filePath} at line {i}", GameComponent.Scripts);
                    }
                }
                else
                {
                    // just end the warp-def & let the line be parsed again
                    --i;
                    ongoingDef = false;
                }
            }

            return npcDefs;
        }

        public WarpDefinition ParseWarpHeaderLine(string line)
        {
            if (line == null || line.Length == 0)
                return null;

            WarpDefinition newDef = new();
            int nextExpectedField = 0;
            for (int searchIdx = 0; searchIdx < line.Length; /*empty*/)
            {
                ReadOnlySpan<char> nextWord = ScriptUtils.GetNextWord(line.AsSpan(), searchIdx, out searchIdx);
                switch (nextExpectedField)
                {
                    case 0: // warp keyword
                        break;
                    case 1: // source map id
                        newDef.SourceMapId = new(nextWord);
                        break;
                    case 2: // min bounds
                        newDef.BoundsMin = new string(nextWord).ToCoordinate();
                        if(newDef.BoundsMin == Coordinate.INVALID)
                        {
                            OwlLogger.LogError($"Malformed Warp: BoundsMin {nextWord.ToString()} is invalid!", GameComponent.Scripts);
                            return null;
                        }
                        break;
                    case 3: // max bounds
                        newDef.BoundsMax = new string(nextWord).ToCoordinate();
                        if (newDef.BoundsMax == Coordinate.INVALID)
                        {
                            OwlLogger.LogError($"Malformed Warp: BoundsMax {nextWord.ToString()} is invalid!", GameComponent.Scripts);
                            return null;
                        }
                        break;
                    case 4: // 
                        newDef.TargetMapCoord = new string(nextWord).ToMapCoordinate();
                        if(newDef.TargetMapCoord == MapCoordinate.INVALID)
                        {
                            OwlLogger.LogError($"Malformed Warp: TargetMapCoord {nextWord.ToString()} is invalid!", GameComponent.Scripts);
                            return null;
                        }
                        break;
                    default:
                        // Means extra-garbage at the end of the header-line - we just discard that
                        OwlLogger.LogF($"Unnecessary data at end of Npc Header Line: {0}", nextWord.ToString(), GameComponent.Scripts, LogSeverity.Verbose);
                        break;
                }
                nextExpectedField++;
            }

            return newDef;
        }
    }
}