using Core.DataTypes;
using Core.Modules.Interfaces;
using System.Text.Json;

namespace Core.Modules
{
    // Serialization and File Operations
    public static class SettingsFileHandler {
        private static readonly JsonSerializerOptions options = new() { WriteIndented = true };

        public static T? LoadFromFile<T>(string fileName) where T : new() {
            try {
                string json = File.ReadAllText(fileName);
                return JsonSerializer.Deserialize<T>(json);
            } catch {
                // Consider logging the error
                return new T();
            }
        }

        public static void SaveToFile<T>(T settings, string fileName) {
            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(fileName, json) ;
        }
    }


    internal class AppSettings : IAppSettings, IDisposable  {
        public event Action<string, int>? LogEvent;
        public List<TDeviceSettings> DeviceSettings { get; set; } = new ();
        public TReactorControlSettings ReactorControlSettings { get; set; } = new ();
        public TSimulationSettings SimSettings { get; set; } = new ();

        private string FilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\EFMoST_ControlAPP";  // Adjust as needed
        private const string FileNameDeviceSettings = "DeviceSettings.json";
        private const string FileNameControlSettings = "ControlSettings.json";
        private const string FileNameSimulationSettings = "SimSettings.json";

        public AppSettings() {
            if (!Directory.Exists(FilePath)) {
                Directory.CreateDirectory(FilePath);
            }
        }

        public void Begin() {
            DeviceSettings = SettingsFileHandler.LoadFromFile<List<TDeviceSettings>>(Path.Combine(FilePath, FileNameDeviceSettings)) ?? new();
            if (DeviceSettings.Count == 0) {
                LogEvent?.Invoke("No device settings found? Using default values...", 0);
                DeviceSettings.Add(new TDeviceSettings { DeviceName = "GasSensor", ConnectionSettings = new TDeviceConnectionSettings { PortName = "Auto", BaudRate = 115200 } });
                DeviceSettings.Add(new TDeviceSettings { DeviceName = "Controller", ConnectionSettings = new TDeviceConnectionSettings { PortName = "Auto", BaudRate = 115200 } });
            }
            ReactorControlSettings = SettingsFileHandler.LoadFromFile<TReactorControlSettings>(Path.Combine(FilePath, FileNameControlSettings)) ?? new();
            SimSettings = SettingsFileHandler.LoadFromFile<TSimulationSettings>(Path.Combine(FilePath, FileNameSimulationSettings)) ?? new();
            LogEvent?.Invoke("Application settings loaded!", 1);
        }

        public void Dispose() {
            SaveSettings();
        }

        public void SaveSettings() {
            LogEvent?.Invoke("Application settings saved!", 1);
            SettingsFileHandler.SaveToFile(DeviceSettings, Path.Combine(FilePath, FileNameDeviceSettings));
            SettingsFileHandler.SaveToFile(ReactorControlSettings, Path.Combine(FilePath, FileNameControlSettings));
            SettingsFileHandler.SaveToFile(SimSettings, Path.Combine(FilePath, FileNameSimulationSettings));
        }
    }
}
