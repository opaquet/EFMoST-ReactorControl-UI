using Core.Modules.Interfaces;

namespace Core.Modules
{
    public class MyEventLogger : IEventLogger, IDisposable {
        public event Action? LogUpdated;

        private class LogEntry {
            public string Label { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public int LogLevel { get; set; } = 0;
            public DateTime Timestamp { get; set; }
        }

        private readonly IFileWriter? _filewriter;
        private Queue<LogEntry> _logEntries = new(500);
        private readonly object _lockObject = new();
        private bool WriteToFile => _filewriter != null;

        public int Count { get => _logEntries.Count; }
        public int MaxLength { get; set; } = 500;

        public MyEventLogger(IFileWriter? fileWriter = null) {
            _filewriter = fileWriter;
        }

        public void Dispose() => _filewriter?.Dispose();

        public void Log(string msg, string label = "", int logLevel = 0) => Append(msg, label, logLevel);

        public void Append(string msg, string label = "", int logLevel = 0) {
            if (msg == null || label == null) {
                return;
            }

            if (msg.Length == 0) {
                return;
            }

            msg = msg.Replace("\n", "").Replace("\r", "");
            msg = msg.TrimEnd('.', ',');

            if (msg.Length == 0) {
                return;
            }

            LogEntry entry = new() { Label = label, Message = msg, LogLevel = logLevel, Timestamp = DateTime.Now};

            lock (_lockObject) {
                _logEntries.Enqueue(entry);
                while (_logEntries.Count > MaxLength) {
                    _logEntries.Dequeue();
                }
            }

            if (WriteToFile) {
                _filewriter?.WriteLine($"{entry.Timestamp:dd.MM.yy HH:mm:ss}:\t{logLevel}\t{label}\t{msg}");
            }

            LogUpdated?.Invoke();
        }

        public void Clear() {
            lock (_lockObject) {
                _logEntries.Clear();
            }
        }

        /// <summary>
        /// Method <b>LogString</b> returns the last "amount" log entries, of which the log level is equal or less than maxLogLevel.
        /// It is returned as a single multiline string for display purposes for example.
        /// </summary>
        /// <param name="amount">Number of elements or lines that should be returned.</param>
        /// <param name="maxLogLevel">highes loglevel. Only values of equal or smaller loglevel are returned</param>
        /// <returns></returns>
        public string LogString(int amount = 50, int maxLogLevel = 0) {
            IEnumerable<string> logs;
            lock (_lockObject) {
                logs = _logEntries
                        .Where(e => e.LogLevel <= maxLogLevel)
                        .Skip(Math.Max(0, _logEntries.Count - amount))
                        .Select(e => $"{e.Timestamp:HH:mm:ss}: {e.Label}    \t{e.Message}");
            }
            try {
                return string.Join("\n", logs);
            } catch (Exception ex) { return $"LogString Error: {ex.Message}"; }
        }
    }
}
