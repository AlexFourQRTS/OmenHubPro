using System;
using System.IO;
using System.Text;

namespace OmenHub.Core.Logging
{
    /// <summary>Appends timestamped lines to logs\omenhub.log next to the executable.</summary>
    public static class AppLog
    {
        private const long MaximumFileBytes = 1000000;
        private const int RepeatSuppressionSeconds = 60;

        private static readonly object writeLock = new object();
        private static string lastMessage = "";
        private static DateTime lastMessageTime = DateTime.MinValue;

        public static string FilePath
        {
            get
            {
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                try { if (!Directory.Exists(logDirectory)) Directory.CreateDirectory(logDirectory); }
                catch { }
                return Path.Combine(logDirectory, "omenhub.log");
            }
        }

        public static void Write(string message)
        {
            try
            {
                lock (writeLock)
                {
                    bool isRepeat = message == lastMessage
                        && (DateTime.Now - lastMessageTime).TotalSeconds < RepeatSuppressionSeconds;
                    if (isRepeat) return;
                    lastMessage = message;
                    lastMessageTime = DateTime.Now;

                    string path = FilePath;
                    if (File.Exists(path) && new FileInfo(path).Length > MaximumFileBytes) File.Delete(path);
                    string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + message + Environment.NewLine;
                    File.AppendAllText(path, line, Encoding.UTF8);
                }
            }
            catch { }
        }
    }
}
