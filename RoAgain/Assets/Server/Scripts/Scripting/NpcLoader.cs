using OwlLogging;
using System;
using System.Collections.Generic;

namespace Server
{
    public class NpcLoader
    {
        public List<NpcDefinition> ParseFile(string filePath)
        {
            List<NpcDefinition> npcDefs = new();
            string[] lines = ScriptUtils.LoadRawLinesUncached(filePath);
            if(lines == null || lines.Length == 0 )
            {
                return npcDefs;
            }

            string firstLine = lines[0];
            int scriptVersion = ScriptUtils.GetScriptVersion(firstLine);
            if(scriptVersion < 0)
            {
                OwlLogger.LogError($"Script at path {filePath} malformed: Script Version Entry.", GameComponent.Scripts);
                return npcDefs;
            }

            bool ongoingNpc = false;
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                if (!lines[i].StartsWith(ScriptKeywords.NpcHeader))
                    continue;

                if(!ongoingNpc)
                {
                    NpcDefinition newDef = ParseNpcHeaderLine(lines[i]);
                    if (newDef != null)
                    {
                        OwlLogger.LogF("Npc Definition found for Npc {0}", newDef.NpcId, GameComponent.Scripts);
                        npcDefs.Add(newDef);
                        ongoingNpc = true;
                    }    
                    else
                    {
                        OwlLogger.LogError($"Malformed NpcHeader in file {filePath} at line {i}", GameComponent.Scripts);
                    }
                }
                else
                {
                    // TODO: Parse the script content of the NPC - assuming that we have the Script & NPC definitions in the same file.
                    // For now, we don't have scripts yet, so we just end the npc & let the line be parsed again
                    --i;
                    ongoingNpc = false;
                }
            }

            return npcDefs;
        }

        public NpcDefinition ParseNpcHeaderLine(string line)
        {
            if (line == null || line.Length == 0)
                return null;

            NpcDefinition newDef = new();
            int nextExpectedField = 0;
            for(int searchIdx = 0; searchIdx < line.Length; /*empty*/)
            {
                ReadOnlySpan<char> nextWord = ScriptUtils.GetNextWord(line.AsSpan(), searchIdx, out searchIdx);
                switch (nextExpectedField)
                {
                    case 0: // npc keyword
                        break;
                    case 1: // npc Id
                        if(!int.TryParse(nextWord, out newDef.NpcId))
                        {
                            OwlLogger.LogError($"Malformed Npc Header: NpcId {nextWord.ToString()} is invalid!", GameComponent.Scripts);
                            return null;
                        }
                        break;
                    case 2: // MapId
                        newDef.Location = new string(nextWord).ToMapCoordinate();
                        if(newDef.Location == MapCoordinate.INVALID)
                        {
                            OwlLogger.LogError($"Malformed Npc Header: Location {nextWord.ToString()} is invalid!", GameComponent.Scripts);
                            return null;
                        }
                        break;
                    case 3: // Model Id
                        if (!int.TryParse(nextWord, out newDef.ModelId))
                        {
                            OwlLogger.LogError($"Malformed Npc Header:  Model Id {nextWord.ToString()} is invalid!", GameComponent.Scripts);
                            return null;
                        }
                        break;
                    case 4: // Name Loc Id
                        if (!LocalizedStringId.TryParse(nextWord, out newDef.NameLocId))
                        {
                            OwlLogger.LogError($"Malformed Npc Header:  Name Localized Id {nextWord.ToString()} is invalid!", GameComponent.Scripts);
                            return null;
                        }
                        break;
                    case 5: // Script Id
                        if (!int.TryParse(nextWord, out newDef.ScriptId))
                        {
                            OwlLogger.LogError($"Malformed Npc Header:  Model Id {nextWord.ToString()} is invalid!", GameComponent.Scripts);
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


