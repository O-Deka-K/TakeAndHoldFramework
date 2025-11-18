using BepInEx.Logging;

namespace TNHFramework.Utilities
{
    static class TNHFrameworkLogger
    {
        public static ManualLogSource BepLog = null;

        public static bool AllowLogging = true;
        public static bool LogCharacter = false;
        public static bool LogFile = false;
        public static bool LogTNH = false;

        public enum LogType
        {
            General,
            Character,
            File,
            TNH
        }

        public static void Init()
        {
            BepLog = Logger.CreateLogSource("TNHFramework");
        }

        public static void Log(string log, LogType type)
        {
            log = "TNHFramework -- " + log;

            if (AllowLogging)
            {
                //log = $"[{DateTime.Now:HH:mm:ss}] {log}";  // Add timestamp

                if (type == LogType.General)
                {
                    BepLog.LogInfo(log);
                }
                else if (type == LogType.Character && LogCharacter)
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
            log = "TNHFramework -- " + log;
            BepLog.LogWarning(log);
        }

        public static void LogError(string log)
        {
            log = "TNHFramework -- " + log;
            BepLog.LogError(log);
        }
    }
}
