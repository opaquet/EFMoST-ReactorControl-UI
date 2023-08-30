using Core.Modules.Interfaces;
using System.Text;

namespace Core.Modules
{
    /// <summary>
    /// Class <b>MultiThreadFileWriter</b> is used to append lines of text to a text file in a thread safe manner.
    /// It creates a new file if the given file does not exist.
    /// Just call the <b>WriteLine</b> Method...
    /// </summary>
    public class FileWriter_MultiThread : FileWriter, IFileWriter {
        private readonly object _lockObject = new();
        private Queue<string> _textToWrite = new();
        private CancellationTokenSource _source = new();
        private SemaphoreSlim _semaphore = new(0);
        private Task _writeTask;
        public TaskStatus WriteTaskStatus => _writeTask.Status;

        /// <summary>
        /// Constructor. Needs the name of the target file to write to.
        /// </summary>
        /// <param name="path">Full path to the file where the strings should be written to.</param>
        public FileWriter_MultiThread(string path, FileWriterMode mode) : base(path, mode) {
            _writeTask = Task.Run(WriteToFileAsync);
        }

        /// <summary>
        /// Stop the background filewriter task properly
        /// </summary>
        public override void Dispose() {
            while (_textToWrite.Count > 0) {
                Thread.Sleep(10);
            }
            _source.Cancel();
            _semaphore.Release();
            _writeTask.Wait();
            _source.Dispose();
            _semaphore.Dispose();
        }

        /// <summary>
        /// Appends a string as a single line to the specified text file (asychronously) 
        /// </summary>
        /// <param name="line">Line of text to append to the file</param>
        public void WriteLine(string line) {
            lock (_lockObject) {
                _textToWrite.Enqueue(line);
            }
            _semaphore.Release();
        }

        /// <summary>
        /// Appends a string array as a multiple line to the specified text file (asychronously) 
        /// </summary>
        /// <param name="line">array with Lines of text to append to the file</param>
        public void WriteLines(string[] lines) {
            lock (_lockObject) {
                StringBuilder sb = new StringBuilder();
                foreach (string line in lines) {
                    sb.AppendLine(line);
                }
                _textToWrite.Enqueue(sb.ToString());
            }
            _semaphore.Release();
        }

        /// <summary>
        /// Appends a stringbuilder as a single or multiple lines to the specified text file (asychronously) 
        /// </summary>
        /// <param name="line">stringbuilder object with text to append to the file</param>
        public void WriteLines(StringBuilder lines) {
            lock (_lockObject) {
                _textToWrite.Enqueue(lines.ToString());
            }
            _semaphore.Release();
        }

        private async Task WriteToFileAsync() {
            try {
                while (!_source.Token.IsCancellationRequested) {
                    await _semaphore.WaitAsync(_source.Token).ConfigureAwait(false);
                    if (_source.Token.IsCancellationRequested) { return; }
                    string? textLine = string.Empty;
                    if (_textToWrite.TryDequeue(out textLine)) {
                        await using StreamWriter w = Mode == FileWriterMode.Append ? File.AppendText(_filepath) : File.CreateText(_filepath);
                        await w.WriteLineAsync(textLine).ConfigureAwait(false);
                        await w.FlushAsync().ConfigureAwait(false);
                    }
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Console.Error.WriteLine($"An error occurred: {ex.Message}");
            } finally {
                _semaphore.Release();
            }
        }
    }
}
