using System;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace OwlLogging
{
    [Flags]
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
        Scripts         = 1 << 14,
        Items           = 1 << 15,

        All = Other | Input | Character | UI | Battle | Network | Grid | Editor | Skill | CellEffect | Chat | Persistence | Config | ChatCommands | Scripts | Items,
    }

    public enum LogSeverity
    {
        VeryVerbose,
        Verbose,
        Log,
        Warning,
        Error
    }

    [Flags]
    public enum LogDetail
    {
        CallerNames = 1,
        CallLocation = 2,
    }

    public class OwlLogger
    {
        public static LogSeverity CurrentLogVerbosity = LogSeverity.Verbose;
        public static LogDetail CurrentLogDetail = LogDetail.CallerNames | LogDetail.CallLocation;
        //public static LogDetail CurrentLogDetail = LogDetail.CallerNames;
        public static GameComponent EnabledComponents = GameComponent.All;
        //public static GameComponent EnabledComponents = GameComponent.Grid | GameComponent.Network;
        private static StringBuilder logBuilder = new(); // this makes the logger non-threadsafe!

        public static void LogError(string message, GameComponent component, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(message, component, LogSeverity.Error, memberName, filePath, lineNumber);
        }

        public static void LogWarning(string message, GameComponent component, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(message, component, LogSeverity.Warning, memberName, filePath, lineNumber);
        }

        public static void Log(string message, GameComponent component, LogSeverity severity = LogSeverity.Log, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (!EnabledComponents.HasFlag(component))
                return;

            if (CurrentLogVerbosity > severity)
                return;

            string fullMessage = ComposeMessage(message, component, severity, memberName, filePath, lineNumber);
            if (severity == LogSeverity.Error)
                Debug.LogError(fullMessage);
            else if (severity == LogSeverity.Warning)
                Debug.LogWarning(fullMessage);
            else
                Debug.Log(fullMessage);
        }

        public static void LogF(string formatString, object arg1, GameComponent component, LogSeverity severity = LogSeverity.Log, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (!EnabledComponents.HasFlag(component))
                return;

            if (CurrentLogVerbosity > severity)
                return;

            string formattedMessage = string.Format(formatString, arg1);
            Log(formattedMessage, component, severity, memberName, filePath, lineNumber);
        }

        public static void LogF(string formatString, object arg1, object arg2, GameComponent component, LogSeverity severity = LogSeverity.Log, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (!EnabledComponents.HasFlag(component))
                return;

            if (CurrentLogVerbosity > severity)
                return;

            string formattedMessage = string.Format(formatString, arg1, arg2);
            Log(formattedMessage, component, severity, memberName, filePath, lineNumber);
        }

        public static void LogF(string formatString, object arg1, object arg2, object arg3, GameComponent component, LogSeverity severity = LogSeverity.Log, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (!EnabledComponents.HasFlag(component))
                return;

            if (CurrentLogVerbosity > severity)
                return;

            string formattedMessage = string.Format(formatString, arg1, arg2, arg3);
            Log(formattedMessage, component, severity, memberName, filePath, lineNumber);
        }

        public static void LogF(string formatString, object arg1, object arg2, object arg3, object arg4, GameComponent component, LogSeverity severity = LogSeverity.Log, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (!EnabledComponents.HasFlag(component))
                return;

            if (CurrentLogVerbosity > severity)
                return;

            string formattedMessage = string.Format(formatString, arg1, arg2, arg3, arg4);
            Log(formattedMessage, component, severity, memberName, filePath, lineNumber);
        }

        public static void LogF(string formatString, object arg1, object arg2, object arg3, object arg4, object arg5, GameComponent component, LogSeverity severity = LogSeverity.Log, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (!EnabledComponents.HasFlag(component))
                return;

            if (CurrentLogVerbosity > severity)
                return;

            string formattedMessage = string.Format(formatString, arg1, arg2, arg3, arg4, arg5);
            Log(formattedMessage, component, severity, memberName, filePath, lineNumber);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        /// <param name="oName"></param>
        /// <param name="caller"></param>
        /// <param name="component"></param>
        /// <returns>o == null</returns>
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
            LogF("Function starting: {0}", memberName, component, LogSeverity.VeryVerbose);
        }

        public static void LogFunctionExit(GameComponent component, [CallerMemberName] string memberName = "")
        {
            LogF("Function exiting: {0}", memberName, component, LogSeverity.VeryVerbose);
        }


        private static string ComposeMessage(string message, GameComponent component, LogSeverity severity, string memberName, string filePath, int lineNumber)
        {
            logBuilder.Clear();
            logBuilder.AppendFormat("{0} - {1}: {2}", severity, component, message);

            if(CurrentLogDetail.HasFlag(LogDetail.CallerNames)
                || CurrentLogDetail.HasFlag(LogDetail.CallLocation))
            {
                logBuilder.Append(" (");
                if (CurrentLogDetail.HasFlag(LogDetail.CallerNames))
                {
                    logBuilder.Append(memberName);
                }

                if(CurrentLogDetail.HasFlag(LogDetail.CallLocation))
                {
                    if (CurrentLogDetail.HasFlag(LogDetail.CallerNames))
                        logBuilder.Append(" ");
                    logBuilder.AppendFormat("@ {0}:{1}", filePath, lineNumber);
                }
                logBuilder.Append(")");
            }

            return logBuilder.ToString();
        }
    }
}
