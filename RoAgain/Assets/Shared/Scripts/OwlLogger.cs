using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace OwlLogging
{
    [System.Flags]
    public enum GameComponent
    {
        Other = 1,
        Input = 2,
        Character = 4,
        UI = 8,
        Battle = 16,
        Network = 32,
        Grid = 64,
        Editor = 128,
        Skill = 256,
        CellEffect = 512,
        Chat = 1024,
        Persistence = 2048,
        Config = 4096,

        All = Other | Input | Character | UI | Battle | Network | Grid | Editor | Skill | CellEffect | Chat | Persistence | Config,
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
            if(o == null)
            {
                LogError($"{caller.GetType()} doesn't have {oName} set!", component);
                return true;
            }
            return false;
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
