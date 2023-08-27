using Core.Modules.Interfaces;

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
        public FileWriter_MultiThread(string path) : base(path) {
            _writeTask = Task.Run(WriteToFile);
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

        private async void WriteToFile() {
            using StreamWriter w = File.AppendText(_filepath);
            while (!_source.IsCancellationRequested) {
                await _semaphore.WaitAsync();
                string? textLine = null;
                lock (_lockObject) {
                    if (_textToWrite.Count > 0) {
                        textLine = _textToWrite.Dequeue();
                    }
                }
                if (textLine != null) {
                    await w.WriteLineAsync(textLine);
                }

                w.Flush();
            }
        }
    }
}
