using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace OwlLogging
{
    [System.Flags]
    public enum GameComponent
    {
        Other           = 1 << 0,
        Input           = 1 << 1,
        Character       = 1 << 2,
        UI              = 1 << 3,
        Battle          = 1 << 4,
        Network         = 1 << 5,
        Grid            = 1 << 6,
        Editor          = 1 << 7,
        Skill           = 1 << 8,
        CellEffect      = 1 << 9,
        Chat            = 1 << 10,
        Persistence     = 1 << 11,
        Config          = 1 << 12,
        ChatCommands    = 1 << 13,

        All = Other | Input | Character | UI | Battle | Network | Grid | Editor | Skill | CellEffect | Chat | Persistence | Config | ChatCommands,
    }

    public enum LogSeverity
    {
        VeryVerbose,
        Verbose,
        Log,
        Warning,
        Error
    }

    [System.Flags]
    public enum LogDetail
    {
        CallerNames = 1,
        CallLocation = 2,
    }

    public class OwlLogger
    {
        public static LogSeverity CurrentLogVerbosity = LogSeverity.Verbose;
        //public static LogDetail CurrentLogDetail = LogDetail.CallerNames | LogDetail.CallLocation;
        public static LogDetail CurrentLogDetail = LogDetail.CallerNames;
        public static GameComponent EnabledComponents = GameComponent.All;
        //public static GameComponent EnabledComponents = GameComponent.Grid | GameComponent.Network;

        public static void LogError(string message, GameComponent component, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (!EnabledComponents.HasFlag(component))
                return;

            if (CurrentLogVerbosity > LogSeverity.Error)
                return;

            string fullmessage = ComposeMessage(message, component, LogSeverity.Error, memberName, filePath, lineNumber);
            Debug.LogError(fullmessage);
        }

        public static void LogWarning(string message, GameComponent component, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (!EnabledComponents.HasFlag(component))
                return;

            if (CurrentLogVerbosity > LogSeverity.Warning)
                return;

            string fullMessage = ComposeMessage(message, component, LogSeverity.Warning, memberName, filePath, lineNumber);
            Debug.LogWarning(fullMessage);
        }

        public static void Log(string message, GameComponent component, LogSeverity severity = LogSeverity.Log, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (!EnabledComponents.HasFlag(component))
                return;

            if (CurrentLogVerbosity > severity)
                return;

            string fullMessage = ComposeMessage(message, component, severity, memberName, filePath, lineNumber);
            Debug.Log(fullMessage);
        }

        public static void LogErrorAndBroadcast<ActionType>(Action<ActionType> action, ActionType value, string message, GameComponent component,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (!string.IsNullOrEmpty(message))
                LogError(message, component, memberName, filePath, lineNumber);

            action?.Invoke(value);
        }

        public static void LogWarningAndBroadcast<ActionType>(Action<ActionType> action, ActionType value, string message, GameComponent component,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (!string.IsNullOrEmpty(message))
                LogWarning(message, component, memberName, filePath, lineNumber);

            action?.Invoke(value);
        }

        public static void LogAndBroadcast<ActionType>(Action<ActionType> action, ActionType value, string message, GameComponent component,
            LogSeverity severity = LogSeverity.Log, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (!string.IsNullOrEmpty(message))
                Log(message, component, severity, memberName, filePath, lineNumber);

            action?.Invoke(value);
        }

        public static bool PrefabNullCheckAndLog(object o, string oName, object caller, GameComponent component)
        {
            if (o == null)
            {
                LogError($"{caller.GetType()} doesn't have {oName} set!", component);
                return true;
            }
            return false;
        }

        public static void LogFunctionEntry(GameComponent component, [CallerMemberName] string memberName = "")
        {
            Log($"Function starting: {memberName}", component, LogSeverity.VeryVerbose);
        }

        public static void LogFunctionExit(GameComponent component, [CallerMemberName] string memberName = "")
        {
            Log($"Function exiting: {memberName}", component, LogSeverity.VeryVerbose);
        }


        private static string ComposeMessage(string message, GameComponent component, LogSeverity severity, string memberName, string filePath, int lineNumber)
        {
            string fullMessage = "";

            if (severity < LogSeverity.Log)
                fullMessage += $"{severity} - ";

            fullMessage += $"{component}: ";

            fullMessage += $"{message}";

            string details = " ";
            if (CurrentLogDetail.HasFlag(LogDetail.CallerNames))
            {
                details += $"{memberName}";
            }

            if (CurrentLogDetail.HasFlag(LogDetail.CallLocation))
            {
                if (details != " ")
                    details += " ";
                details += $"@ {filePath}:{lineNumber}";
            }

            if (details != " ")
            {
                fullMessage += $"({details})";
            }

            return fullMessage;
        }
    }
}
