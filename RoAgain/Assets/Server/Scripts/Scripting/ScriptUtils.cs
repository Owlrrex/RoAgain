using OwlLogging;
using System;
using System.IO;

namespace Server
{
    public static class ScriptUtils
    {
        public static ReadOnlySpan<char> GetNextWord(ReadOnlySpan<char> data, int start, out int end)
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

        public static int GetScriptVersion(string versionLine)
        {
            if (!versionLine.StartsWith(ScriptKeywords.NpcScriptVersion))
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
    }
}