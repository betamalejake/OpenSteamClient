using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.VisualBasic;
using OpenSteamClient.Logging;

namespace OpenSteamworks.Client;

public class Logger : ILogger {
    private struct LogData {
        public Logger Logger;
        public DateTime Timestamp;
        public LogLevel Level;
        public string Message;
        public string Category;
        public bool FullLine;

        public LogData(Logger logger, LogLevel level, string msg, string category, bool fullLine) {
            this.Logger = logger;
            this.Timestamp = DateTime.Now;
            this.Level = level;
            this.Message = msg;
            this.Category = category;
            this.FullLine = fullLine;
        }
    }

    public static ILogger GeneralLogger {
        get {
            if (s_generalLoggerOverride != null) {
                return s_generalLoggerOverride;
            }

            return s_lazyGeneralLogger.Value;
        }

        internal set {
            s_generalLoggerOverride = value;
        }
    }

    private static ILogger? s_generalLoggerOverride;
    private readonly static Lazy<ILogger> s_lazyGeneralLogger = new(() => new Logger("General"));

    public string Name { get; set; } = "";
    public string? LogfilePath { get; init; } = null;

    /// <summary>
    /// Should the logger prefix messages it receives?
    /// </summary>
    public bool AddPrefix { get; set; } = true;

    /// <summary>
    /// If this logger is a sublogger, this is it's name.
    /// </summary>
    private string SubLoggerName { get; set; } = "";

    /// <summary>
    /// If this logger is a sublogger, this is it's parent.
    /// </summary>
    private Logger? ParentLogger { get; set; }

    // https://no-color.org/
    private static bool s_disableColors = Environment.GetEnvironmentVariable("NO_COLOR") != null;
    private object _logStreamLock = new();
    private FileStream? _logStream;
    private static List<Logger> s_loggers = new();
    private static readonly Thread s_logThread;

    public class DataReceivedEventArgs : EventArgs {
        public LogLevel Level { get; set; }
        public string Text { get; set; }
        public string AnsiColorSequence { get; set; }
        public string AnsiResetCode { get; set; }
        public bool FullLine { get; set; }
        public DataReceivedEventArgs(LogLevel level, string text, string ansiColorSequence, string ansiResetCode) {
            this.Level = level;
            this.Text = text;
            this.FullLine = text.EndsWith(Environment.NewLine);
            this.AnsiColorSequence = ansiColorSequence;
            this.AnsiResetCode = ansiResetCode;
        }
    }

    public static event EventHandler<DataReceivedEventArgs>? DataReceived;

    static Logger() {
        s_logThread = new(LogThreadMain);
        s_logThread.Name = "LogThread";
        s_logThread.Start();
    }

    private Logger(string name, string? filepath = "") {
        this.Name = name;
        this.LogfilePath = filepath;

        s_loggers.Add(this);
        if (!string.IsNullOrEmpty(filepath)) {
            if (File.Exists(filepath)) {
                // Delete if over 4MB
                var fi = new FileInfo(filepath);
                if ((fi.Length / 1024 / 1024) > 4) {
                    fi.Delete();
                }
            }
            _logStream = File.Open(filepath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            _logStream.Seek(_logStream.Length, SeekOrigin.Begin);
        }

        if (!s_hasRanWindowsHack && OperatingSystem.IsWindows()) {
            RunWindowsConsoleColorsHack();
        }
    }

    private static readonly ConcurrentQueue<LogData> s_dataToLog = new();
    private static void LogThreadMain() {
        while (true)
        {
            if (s_dataToLog.IsEmpty) {
                Thread.Sleep(1);
                continue;
            }

            if (s_dataToLog.TryDequeue(out LogData data)) {
                if (data.FullLine) {
                    MessageInternal(data.Logger, data.Timestamp, data.Level, data.Message, data.Category);
                } else {
                    WriteInternal(data.Logger, data.Message);
                }
            }
        }
    }

    public static Logger GetLogger(string name, string? filepath = "") {
        foreach (var item in s_loggers)
        {
            if (item.Name == name && item.LogfilePath == filepath) {
                return item;
            }
        }

        return new Logger(name, filepath);
    }

    /// <summary>
    /// Creates a sub-logger. Uses the logstream of the current logger, and sets subname as a category name for each print.
    /// </summary>
    /// <param name="subName"></param>
    /// <returns></returns>
    public Logger CreateSubLogger(string subName) {
        var logger = new Logger("", "");
        logger.SubLoggerName = subName;
        logger.ParentLogger = this;
        return logger;
    }

    private static void MessageInternal(Logger logger, DateTime timestamp, LogLevel level, string message, string category) {
        if (logger.ParentLogger != null) {
            var actualCategory = logger.SubLoggerName;
            if (!string.IsNullOrEmpty(category)) {
                actualCategory += "/" + category;
            }

            MessageInternal(logger.ParentLogger, timestamp, level, message, actualCategory);
            return;
        }

        string formatted = message;
        if (logger.AddPrefix) {
            // welp. we can't just use the system's date format, but we also need to use the system's time at the same time, which won't include milliseconds and will always have AM/PM appended, even on 24-hour clocks. So use the objectively better formatting system of dd/MM/yyyy and always use 24-hour time (which will also make it easier for the devs reviewing bug reports)
            formatted = $"[{timestamp:dd/MM/yyyy HH:mm:ss.ff} {logger.Name}{(string.IsNullOrEmpty(category) ? "" : $"/{category}")}: {level.ToString()}] {message}";
        }

		if (!formatted.EndsWith(Environment.NewLine)) {
			formatted += Environment.NewLine;
		}

        string ansiColorCode = string.Empty;
        string ansiResetCode = string.Empty;

        if (!s_disableColors) {
            ansiResetCode = "\x1b[0m";
            if (level == LogLevel.Fatal) {
                ansiColorCode = "\x1b[91m";
            } else if (level == LogLevel.Error) {
                ansiColorCode = "\x1b[31m";
            } else if (level == LogLevel.Warning) {
                ansiColorCode = "\x1b[33m";
            } else if (level == LogLevel.Info) {
                //ansiColorCode = "\x1b[37m";
            } else if (level == LogLevel.Debug) {
                ansiColorCode = "\x1b[2;37m";
            }
        }

        Console.Write(ansiColorCode + formatted + ansiResetCode);

        if (logger._logStream != null) {
            lock (logger._logStreamLock)
            {
                logger._logStream.Write(Encoding.UTF8.GetBytes(formatted));

                //TODO: Implement a more robust system with debouncing and/or per lines since last flush
                logger._logStream.Flush();
            }
        }

        DataReceived?.Invoke(logger, new(level, formatted, ansiColorCode, ansiResetCode));
    }

    private void AddData(LogLevel level, string message) {
		foreach (var line in message.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
		{
			s_dataToLog.Enqueue(new LogData(this, level, line, string.Empty, true));
		}

    }

	private static void WriteInternal(Logger logger, string message) {
        Console.Write(message);
        if (logger._logStream != null) {
            lock (logger._logStreamLock)
            {
                logger._logStream.Write(Encoding.Default.GetBytes(message));
            }
        }

        DataReceived?.Invoke(logger, new(LogLevel.Info, message, string.Empty, string.Empty));
    }

    /// <inheritdoc/>
    public void Write(LogLevel level, string message) {
        AddData(level, message);
    }

    ~Logger() {
        s_loggers.Remove(this);
    }

    private static bool s_hasRanWindowsHack = false;

    /// <summary>
    /// Windows is stuck using legacy settings unless you tell it explicitly to use "ENABLE_VIRTUAL_TERMINAL_PROCESSING". Why???
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static void RunWindowsConsoleColorsHack() {
        s_hasRanWindowsHack = true;
        const int STD_INPUT_HANDLE = -10;
        const int STD_OUTPUT_HANDLE = -11;
        const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;
        const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

        var iStdIn = GetStdHandle(STD_INPUT_HANDLE);
        var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);

        if (!GetConsoleMode(iStdIn, out uint inConsoleMode))
        {
            Console.WriteLine("[Windows Console Color Hack] failed to get input console mode");
            s_disableColors = true;
            return;
        }
        if (!GetConsoleMode(iStdOut, out uint outConsoleMode))
        {
            Console.WriteLine("[Windows Console Color Hack] failed to get output console mode");
            s_disableColors = true;
            return;
        }

        inConsoleMode |= ENABLE_VIRTUAL_TERMINAL_INPUT;
        outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;

        if (!SetConsoleMode(iStdIn, inConsoleMode))
        {
            Console.WriteLine($"[Windows Console Color Hack] failed to set input console mode, error code: {GetLastError()}");
            s_disableColors = true;
            return;
        }

        if (!SetConsoleMode(iStdOut, outConsoleMode))
        {
            Console.WriteLine($"[Windows Console Color Hack] failed to set output console mode, error code: {GetLastError()}");
            s_disableColors = true;
            return;
        }
    }
}
