using System;
namespace GameEngine
{
    public class Logger
    {
        public enum LogLevel: ushort
        {
            NONE = 0,
            ERROR = 1,
            WARNING = 2,
            INFO = 3,
            DEBUG = 4
        }
        private static LogLevel CURRENT_LOG_LEVEL = LogLevel.INFO;
       
        public static void Error(String message) {
            if (CURRENT_LOG_LEVEL >= LogLevel.ERROR) {
                UnityEngine.Debug.Log(message);
            }
        }
        public static void Warn(String message) {
            if (CURRENT_LOG_LEVEL >= LogLevel.WARNING) {
                UnityEngine.Debug.Log(message);
            }
        }
        public static void Info(String message) {
            if (CURRENT_LOG_LEVEL >= LogLevel.INFO) {
                UnityEngine.Debug.Log(message);
            }
        }
        public static void Debug(String message) {
            if (CURRENT_LOG_LEVEL >= LogLevel.ERROR) {
                UnityEngine.Debug.Log(message);
            }
        }


        
    }
}
