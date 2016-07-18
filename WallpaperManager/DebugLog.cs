using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WallpaperManager
{
    class DebugLog {

        private static string LogTime() {
            DateTime time = DateTime.Now;
            return string.Format("[{0}:{1}:{2}]", time.Hour.ToString("00"), time.Minute.ToString("00"), time.Second.ToString("00"));
        }

        public static void Log(string message) {
            Console.WriteLine(LogTime() + "[Info] " + message);
        }

        public static void Warning(string message) {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(LogTime() + "[Warning] " + message);
            Console.ResetColor();
        }

        public static void Error(string message) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(LogTime() + "[Error] " + message);
            Console.ResetColor();
        }
    }
}
