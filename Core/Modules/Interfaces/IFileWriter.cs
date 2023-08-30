using System.Text;

namespace Core.Modules.Interfaces {

    public enum FileWriterMode {
        Append,
        Replace
    }

    public interface IFileWriter : IDisposable {
        public string FileName { get; }
        public FileWriterMode Mode { get; }

        void WriteLine(string line);

        void WriteLines(string[] lines);

        void WriteLines(StringBuilder lines);
    }

    public abstract class FileWriter
    {
        protected string _filepath = "";

        public string FileName { get => _filepath; }
        public FileWriterMode Mode { get; private set; }

        public abstract void Dispose();

        public FileWriter(string filename, FileWriterMode mode = FileWriterMode.Append)  {
            Mode = mode;

            _filepath = filename;
            string directoryName = Path.GetDirectoryName(filename) ?? string.Empty;
            string FileName = Path.GetFileNameWithoutExtension(filename);

            // erstelle Verzeichnis, wenn nicht vorhanden
            if (!Directory.Exists(directoryName)) {
                Directory.CreateDirectory(directoryName);
            }

            // erstelle "old" Unterverzeichnis, wenn nicht vorhanden.
            if (!Directory.Exists(directoryName + "\\old")) {
                Directory.CreateDirectory(directoryName + "\\old");
            }

            // wenn Datei bereits vorhanden, kopiere diese in das "old" Unterverzeichnis und benenne Sie um.
            // Dabei werden die Dateien fortlaufend numeriert, so dass keine alte Datei überschrieben wird
            if (File.Exists(filename))
            {
                int FNindex = 1;
                string fname = directoryName + "\\old\\" + FileName + FNindex.ToString("D3") + ".log";
                while (File.Exists(fname))
                {
                    FNindex++;
                    fname = directoryName + "\\old\\" + FileName + FNindex.ToString("D3") + ".log";
                }
                File.Move(filename, fname);
            }
        }
    }
}
