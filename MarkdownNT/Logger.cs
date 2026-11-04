using System;
using System.IO;
using System.Text;

namespace MarkdownNT
{
    /// <summary>
    /// 运行时日志：每次启动在程序目录新建 MDNTlog.log，记录软件运行期间的关键事件。
    /// </summary>
    public static class Logger
    {
        private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "MDNTlog.log");

        public static string LogFile => LogPath;

        /// <summary>每次启动重建日志文件，写入会话头。</summary>
        public static void StartSession()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("========================================================");
                sb.AppendLine($"MarkdownNT 运行日志  开始于 {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"程序目录: {AppContext.BaseDirectory}");
                sb.AppendLine("========================================================");
                File.WriteAllText(LogPath, sb.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"日志初始化失败: {ex.Message}");
            }
        }

        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} [INFO ] {message}{Environment.NewLine}");
            }
            catch { }
        }

        public static void Error(string message, Exception? ex = null)
        {
            try
            {
                File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} [ERROR] {message}{(ex == null ? "" : $" - {ex}")}{Environment.NewLine}");
            }
            catch { }
        }
    }
}
