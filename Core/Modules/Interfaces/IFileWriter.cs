namespace Core.Modules.Interfaces {
    public interface IFileWriter : IDisposable
    {
        void WriteLine(string line);
    }

    public abstract class FileWriter
    {
        protected string _filepath = "";

        public abstract void Dispose();

        public FileWriter(string filename)
        {
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
