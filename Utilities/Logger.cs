using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TNHFramework.Utilities
{
    static class TNHTweakerLogger
    {
        public static ManualLogSource BepLog = null;

        public static bool AllowLogging = true;
        public static bool LogCharacter = true;
        public static bool LogFile = true;
        public static bool LogTNH = true;

        public enum LogType
        {
            General,
            Character,
            File,
            TNH
        }

        public static void Init()
        {
            BepLog = BepInEx.Logging.Logger.CreateLogSource("TNHFramework");
        }

        public static void Log(string log, LogType type)
        {
            if (AllowLogging)
            {
                //log = $"[{DateTime.Now:HH:mm:ss}] {log}";  // Add timestamp

                if (type == LogType.General)
                {
                    BepLog.LogInfo(log);
                }
                else if(type == LogType.Character && LogCharacter)
                {
                    BepLog.LogInfo(log);
                }
                else if (type == LogType.File && LogFile)
                {
                    BepLog.LogInfo(log);
                }
                else if (type == LogType.TNH && LogTNH)
                {
                    BepLog.LogInfo(log);
                }
            }
        }

        public static void LogWarning(string log)
        {
            BepLog.LogWarning(log);
        }

        public static void LogError(string log)
        {
            BepLog.LogError(log);
        }

    }
}
