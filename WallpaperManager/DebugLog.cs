using System;
using System.Diagnostics;

namespace WallpaperManager
{
    class DebugLog {

        private static string LogTime() {
            DateTime time = DateTime.Now;
            return string.Format("[{0}:{1}:{2}]", time.Hour.ToString("00"), time.Minute.ToString("00"), time.Second.ToString("00"));
        }

        [Conditional("DEBUG")]
        public static void Log(string message) {
            Console.WriteLine(LogTime() + "[Info] " + message);
        }

        [Conditional("DEBUG")]
        public static void Warning(string message) {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(LogTime() + "[Warning] " + message);
            Console.ResetColor();
        }

        [Conditional("DEBUG")]
        public static void Error(string message) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(LogTime() + "[Error] " + message);
            Console.ResetColor();
        }
    }
}
