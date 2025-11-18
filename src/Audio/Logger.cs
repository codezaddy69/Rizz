using System;
using System.IO;

namespace DJMixMaster.Audio
{
    public static class Logger
    {
        private static readonly string LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "mix.log");
        private static readonly object LockObj = new object();

        static Logger()
        {
            try
            {
                if (!File.Exists(LogFile))
                {
                    using (StreamWriter sw = File.CreateText(LogFile))
                    {
                        sw.WriteLine($"=== DJ Mix Master Log Started at {DateTime.Now} ===");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize logger: {ex.Message}");
            }
        }

        public static void Log(string message, LogLevel level = LogLevel.Info, Exception? ex = null)
        {
            try
            {
                string formattedMessage = ex != null
                    ? $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message} - Exception: {ex.GetType().Name} - {ex.Message}\nStack Trace: {ex.StackTrace}"
                    : $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";

                lock (LockObj)
                {
                    File.AppendAllText(LogFile, formattedMessage + Environment.NewLine);
                }

                // Also write to console for debugging
                Console.WriteLine(formattedMessage);
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"Failed to write to log: {logEx.Message}");
            }
        }

        public static void LogDebug(string message) => Log(message, LogLevel.Debug);
        public static void LogInfo(string message) => Log(message, LogLevel.Info);
        public static void LogWarning(string message) => Log(message, LogLevel.Warning);
        public static void LogError(string message, Exception? ex = null) => Log(message, LogLevel.Error, ex);
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}
