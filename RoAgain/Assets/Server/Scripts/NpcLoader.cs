using OwlLogging;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Server
{
    public class NpcDefinition
    {
        public int NpcId;
        public string MapId;
        public Vector2Int Coordinates;
        public int ModelId;
        public LocalizedStringId NameLocId;
        public int ScriptId;
    }

    public class NpcLoader
    {
        public List<NpcDefinition> ParseFile(string filePath)
        {
            List<NpcDefinition> npcDefs = new();
            string[] lines = LoadRawLinesUncached(filePath);
            if(lines == null || lines.Length == 0 )
            {
                return npcDefs;
            }

            string firstLine = lines[0];
            int scriptVersion = GetScriptVersion(firstLine);
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

                if (lines[i].StartsWith("//"))
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

        public static string[] LoadRawLinesUncached(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                OwlLogger.LogError("Can't load lines for empty path!", GameComponent.Other);
                return null;
            }

            if (File.Exists(path))
            {
                try
                {
                    return File.ReadAllLines(path);
                }
                catch (Exception e)
                {
                    OwlLogger.LogError($"Can't read lines from file {path}: {e.Message}", GameComponent.Other);
                    return null;
                }
            }
            else
            {
                OwlLogger.LogError($"File not found at path {path}!", GameComponent.Other);
                return null;
            }
        }

        public int GetScriptVersion(string versionLine)
        {
            if (!versionLine.StartsWith(ScriptCommandStrings.NpcScriptVersion))
            {
                OwlLogger.LogError($"Script malformed: Has to contain script version entry in its first line!", GameComponent.Scripts);
                return -1;
            }

            string[] firstLineParts = versionLine.Split(" ");
            if (firstLineParts.Length < 2)
            {
                OwlLogger.LogError($"Script malformed: Script version entry doesn't have 2 parts!", GameComponent.Scripts);
                return -1;
            }

            if (!int.TryParse(firstLineParts[1], out var version))
            {
                OwlLogger.LogError($"Script malformed: Script version is not a number!", GameComponent.Scripts);
                return -1;
            }

            // Any script-version specific logic here.
            // Maybe do some version-specific things like: provide an interpreter object, info about automatic script upgrading, etc.
            return version;
        }

        public NpcDefinition ParseNpcHeaderLine(string line)
        {
            if (line == null || line.Length == 0)
                return null;

            NpcDefinition newDef = new();
            int nextExpectedField = 0;
            for(int searchIdx = 0; searchIdx < line.Length; /*empty*/)
            {
                ReadOnlySpan<char> nextWord = GetNextWord(line.AsSpan(), searchIdx, out searchIdx);
                switch (nextExpectedField)
                {
                    case 0: // npc keyword
                        if(!nextWord.SequenceEqual(ScriptCommandStrings.KeywordNpcHeader))
                        {
                            OwlLogger.LogError($"Malformed Npc Header: Missing npc-header keyword!", GameComponent.Scripts);
                            return null;
                        }
                        break;
                    case 1: // npc Id
                        if(!int.TryParse(nextWord, out newDef.NpcId))
                        {
                            OwlLogger.LogError($"Malformed Npc Header: NpcId {nextWord.ToString()} is invalid!", GameComponent.Scripts);
                            return null;
                        }
                        break;
                    case 2: // MapId
                        newDef.MapId = new(nextWord);
                        break;
                    case 3: // Coordinates x
                        if (!int.TryParse(nextWord, out int x))
                        {
                            OwlLogger.LogError($"Malformed Npc Header: Coordinate X {nextWord.ToString()} is invalid!", GameComponent.Scripts);
                            return null;
                        }
                        newDef.Coordinates.x = x;
                        break;
                    case 4: // Coordinates y
                        if (!int.TryParse(nextWord, out int y))
                        {
                            OwlLogger.LogError($"Malformed Npc Header: Coordinate Y {nextWord.ToString()} is invalid!", GameComponent.Scripts);
                            return null;
                        }
                        newDef.Coordinates.y = y;
                        break;
                    case 5: // Model Id
                        if (!int.TryParse(nextWord, out newDef.ModelId))
                        {
                            OwlLogger.LogError($"Malformed Npc Header:  Model Id {nextWord.ToString()} is invalid!", GameComponent.Scripts);
                            return null;
                        }
                        break;
                    case 6: // Name Loc Id
                        if (!LocalizedStringId.TryParse(nextWord, out newDef.NameLocId))
                        {
                            OwlLogger.LogError($"Malformed Npc Header:  Name Localized Id {nextWord.ToString()} is invalid!", GameComponent.Scripts);
                            return null;
                        }
                        break;
                    case 7: // Script Id
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

        public ReadOnlySpan<char> GetNextWord(ReadOnlySpan<char> data, int start, out int end)
        {
            // TODO: Param validations
            bool hasStartedWord = false;
            int wordStart = start;
            for (end = start; end < data.Length; end++)
            {
                if (char.IsWhiteSpace(data[end]))
                {
                    if (!hasStartedWord)
                        continue; // leading whitespace, skip through
                    else
                        break; // whitespace after a word, finish search
                }

                if (!hasStartedWord)
                {
                    wordStart = end;
                    hasStartedWord = true;
                }
            }
            end++; // this makes it so the next search startd at this value doesn't check the whitespace that terminated this word again (optimization)
            return data.Slice(wordStart, end - wordStart - 1); // have to subtract one for the increase we did in the line above
        }
    }
}


