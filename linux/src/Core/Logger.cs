using System;
using System.IO;

namespace Tepi.Linux.Core
{
    public static class Logger
    {
        public static bool IsDebug { get; set; }
        public static bool IsOpaque { get; set; }

        private static readonly string LogDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local", "share", "tepi");
        private static readonly string LogFile = Path.Combine(LogDir, "tepi.log");
        private static readonly object SyncLock = new();

        public static void Init(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.Equals("--debug", StringComparison.OrdinalIgnoreCase))
                    IsDebug = true;
                else if (arg.Equals("--opaque", StringComparison.OrdinalIgnoreCase))
                    IsOpaque = true;
            }

            // Auto-check for VNC without compositor if not explicitly set
            if (!IsOpaque)
            {
                string vnc = Environment.GetEnvironmentVariable("VNCDESKTOP") ?? "";
                string display = Environment.GetEnvironmentVariable("DISPLAY") ?? "";
                if (!string.IsNullOrEmpty(vnc) || display.StartsWith(":1") || display.StartsWith(":2"))
                {
                    Log("VNC display detected. If you notice black borders, run with --opaque");
                }
            }

            Log($"Tepi Linux starting. Debug={IsDebug}, Opaque={IsOpaque}");
        }

        public static void Log(string message)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
            if (IsDebug)
            {
                Console.WriteLine(line);
            }

            try
            {
                lock (SyncLock)
                {
                    Directory.CreateDirectory(LogDir);
                    File.AppendAllText(LogFile, line + Environment.NewLine);
                }
            }
            catch { }
        }

        public static void LogError(string context, Exception ex)
        {
            Log($"ERROR [{context}]: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }
    }
}
