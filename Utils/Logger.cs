using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace ConsoleApp1.Utils
{
    /// <summary>
    /// Thread-safe logger với hỗ trợ nhiều log levels và formatting
    /// </summary>
    internal class Logger
    {
        private static readonly object _lock = new object();
        private static LogLevel _minLogLevel = LogLevel.Debug;
        private static bool _enableFileLogging = false;
        private static string _logFilePath = "app.log";
        private static bool _showTimestamp = true;
        private static bool _showLogLevel = true;

        public enum LogLevel
        {
            Debug = 0,
            Info = 1,
            Success = 2,
            Warn = 3,
            Error = 4,
            Fatal = 5
        }

        #region Configuration
        /// <summary>
        /// Cấu hình log level tối thiểu. Các log dưới level này sẽ không được hiển thị
        /// </summary>
        public static void SetMinLogLevel(LogLevel level)
        {
            _minLogLevel = level;
        }

        /// <summary>
        /// Bật/tắt ghi log ra file
        /// </summary>
        public static void EnableFileLogging(bool enable, string filePath = null)
        {
            _enableFileLogging = enable;
            if (!string.IsNullOrEmpty(filePath))
                _logFilePath = filePath;
        }

        /// <summary>
        /// Cấu hình hiển thị timestamp và log level
        /// </summary>
        public static void Configure(bool showTimestamp = true, bool showLogLevel = true)
        {
            _showTimestamp = showTimestamp;
            _showLogLevel = showLogLevel;
        }
        #endregion

        #region Core Write Method
        private static void Write(
            LogLevel level,
            string levelText,
            string message,
            ConsoleColor color,
            Exception exception = null,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0
        )
        {
            // Kiểm tra log level threshold
            if (level < _minLogLevel)
                return;

            lock (_lock)
            {
                try
                {
                    var logMessage = FormatLogMessage(levelText, message, exception,
                        callerName, callerFilePath, callerLineNumber);

                    // Console output
                    WriteToConsole(levelText, logMessage, color);

                    // File output (nếu được bật)
                    if (_enableFileLogging)
                        WriteToFile(levelText, logMessage);
                }
                catch (Exception ex)
                {
                    // Fallback logging nếu có lỗi trong quá trình log
                    Console.WriteLine($"[LOGGER ERROR] {ex.Message}");
                }
            }
        }

        private static string FormatLogMessage(
            string level,
            string message,
            Exception exception,
            string callerName,
            string callerFilePath,
            int callerLineNumber
        )
        {
            var parts = new System.Text.StringBuilder();

            if (_showTimestamp)
            {
                parts.Append($"[{DateTime.Now:HH:mm:ss.fff}] ");
            }

            if (_showLogLevel)
            {
                parts.Append($"[{level}] ");
            }

            parts.Append(message);

            // Thêm thông tin exception nếu có
            if (exception != null)
            {
                parts.AppendLine();
                parts.Append($"  Exception: {exception.GetType().Name}: {exception.Message}");
                if (!string.IsNullOrEmpty(exception.StackTrace))
                {
                    parts.AppendLine();
                    parts.Append($"  StackTrace: {exception.StackTrace}");
                }
                if (exception.InnerException != null)
                {
                    parts.AppendLine();
                    parts.Append($"  InnerException: {exception.InnerException.Message}");
                }
            }

            return parts.ToString();
        }

        private static void WriteToConsole(string level, string message, ConsoleColor color)
        {
            if (_showTimestamp)
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"[{timestamp}] ");
            }

            if (_showLogLevel)
            {
                Console.ForegroundColor = color;
                Console.Write($"[{level}] ");
            }

            Console.ResetColor();
            Console.WriteLine(message);
        }

        private static void WriteToFile(string level, string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logLine = $"[{timestamp}] [{level}] {message}";
                File.AppendAllText(_logFilePath, logLine + Environment.NewLine);
            }
            catch
            {
                // Không throw exception từ logger
            }
        }
        #endregion

        #region Public Log Methods with String Formatting

        /// <summary>
        /// Log thông tin debug (chỉ trong Debug build)
        /// </summary>
        [Conditional("DEBUG")]
        public static void Debug(string message)
        {
            Write(LogLevel.Debug, "DEBUG", message, ConsoleColor.Magenta);
        }

        [Conditional("DEBUG")]
        public static void Debug(string format, params object[] args)
        {
            Write(LogLevel.Debug, "DEBUG", string.Format(format, args), ConsoleColor.Magenta);
        }

        [Conditional("DEBUG")]
        public static void Debug(Exception ex, string message = null)
        {
            Write(LogLevel.Debug, "DEBUG", message ?? "Exception occurred", ConsoleColor.Magenta, ex);
        }

        /// <summary>
        /// Log thông tin chung
        /// </summary>
        public static void Info(string message)
        {
            Write(LogLevel.Info, "INFO", message, ConsoleColor.Cyan);
        }

        public static void Info(string format, params object[] args)
        {
            Write(LogLevel.Info, "INFO", string.Format(format, args), ConsoleColor.Cyan);
        }

        /// <summary>
        /// Log thông báo thành công
        /// </summary>
        public static void Success(string message)
        {
            Write(LogLevel.Success, "OK", message, ConsoleColor.Green);
        }

        public static void Success(string format, params object[] args)
        {
            Write(LogLevel.Success, "OK", string.Format(format, args), ConsoleColor.Green);
        }

        /// <summary>
        /// Log cảnh báo
        /// </summary>
        public static void Warn(string message)
        {
            Write(LogLevel.Warn, "WARN", message, ConsoleColor.Yellow);
        }

        public static void Warn(string format, params object[] args)
        {
            Write(LogLevel.Warn, "WARN", string.Format(format, args), ConsoleColor.Yellow);
        }

        public static void Warn(Exception ex, string message = null)
        {
            Write(LogLevel.Warn, "WARN", message ?? "Warning occurred", ConsoleColor.Yellow, ex);
        }

        /// <summary>
        /// Log lỗi
        /// </summary>
        public static void Error(string message)
        {
            Write(LogLevel.Error, "ERROR", message, ConsoleColor.Red);
        }

        public static void Error(string format, params object[] args)
        {
            Write(LogLevel.Error, "ERROR", string.Format(format, args), ConsoleColor.Red);
        }

        public static void Error(Exception ex, string message = null)
        {
            Write(LogLevel.Error, "ERROR", message ?? "Error occurred", ConsoleColor.Red, ex);
        }

        /// <summary>
        /// Log lỗi nghiêm trọng
        /// </summary>
        public static void Fatal(string message)
        {
            Write(LogLevel.Fatal, "FATAL", message, ConsoleColor.DarkRed);
        }

        public static void Fatal(string format, params object[] args)
        {
            Write(LogLevel.Fatal, "FATAL", string.Format(format, args), ConsoleColor.DarkRed);
        }

        public static void Fatal(Exception ex, string message = null)
        {
            Write(LogLevel.Fatal, "FATAL", message ?? "Fatal error occurred", ConsoleColor.DarkRed, ex);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Log với custom color
        /// </summary>
        public static void Custom(string level, string message, ConsoleColor color)
        {
            Write(LogLevel.Info, level, message, color);
        }

        /// <summary>
        /// Ghi separator line
        /// </summary>
        public static void Separator(char character = '-', int length = 80)
        {
            lock (_lock)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(new string(character, length));
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Ghi header với border
        /// </summary>
        public static void Header(string title)
        {
            lock (_lock)
            {
                var separator = new string('=', title.Length + 4);
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine(separator);
                Console.WriteLine($"  {title}");
                Console.WriteLine(separator);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Clear console
        /// </summary>
        public static void Clear()
        {
            Console.Clear();
        }

        #endregion
    }
}