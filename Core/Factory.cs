using Core.DataTypes;
using Core.Devices;
using Core.Modules;
using Core.Modules.Interfaces;

namespace Core
{
    internal static class Factory {
        private const string GASSENSOR_DATA_FILENAME = "GasSensorData.csv";
        private const string CONTROLLER_DATA_FILENAME = "ControllerData.csv";
        private const string RELAY_DATA_FILENAME = "RelayData.csv";
        private const string COMPUTED_DATA_FILENAME = "Ethanol.csv";
        private const string LOG_FOLDER_PATH = @"EFMoST_ControlAPP\LogFiles\";
        private const string DATA_FOLDER_PATH = @"EFMoST_ControlAPP\DataFiles\";
        private const string DEFAULT_LOG_FILE = "program.log";
        private static readonly string[] LOG_FILE_NAMES = { "GasSensor.log", "Controller.log" };


        internal static Manager CreateManager() {
            Manager manager = new();
            return manager;
        }


        internal static List<IEventLogger> CreateAllEventLoggers() {
            EnsureLogDirectoryExists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), LOG_FOLDER_PATH));
            return LOG_FILE_NAMES.Select(fileName => CreateEventLogger(fileName)).ToList();
        }

        internal static IEventLogger CreateEventLogger(string fileName = DEFAULT_LOG_FILE) {
            string desktopFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), LOG_FOLDER_PATH);
            return new MyEventLogger(new FileWriter_MultiThread(Path.Combine(desktopFolder, fileName), FileWriterMode.Append));
        }

        internal static IDataHandler CreateDataHandler() {
            string desktopFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), DATA_FOLDER_PATH);
            IFileWriter gasSensorFileWriter = new FileWriter_MultiThread(Path.Combine(desktopFolder, GASSENSOR_DATA_FILENAME), FileWriterMode.Replace);
            IFileWriter controllerFileWriter = new FileWriter_MultiThread(Path.Combine(desktopFolder, CONTROLLER_DATA_FILENAME), FileWriterMode.Replace);
            IFileWriter relayFileWriter = new FileWriter_MultiThread(Path.Combine(desktopFolder, RELAY_DATA_FILENAME), FileWriterMode.Replace);
            IFileWriter computedFileWriter = new FileWriter_MultiThread(Path.Combine(desktopFolder, COMPUTED_DATA_FILENAME), FileWriterMode.Replace);
            return new DataHandler(gasSensorFileWriter, controllerFileWriter, relayFileWriter, computedFileWriter);
        }

        internal static IAppSettings CreateAppSettings() {
            return new AppSettings();
        }

        internal static IDeviceHandler CreateDeviceHandler(IAppSettings? appSettings, List<IEventLogger> eventLoggers) {
            return (appSettings == null)
                ? throw new NullReferenceException()
                : (IDeviceHandler)new DeviceHandler(CreateDeviceList(GetDeviceSettings(appSettings)), eventLoggers);
        }

        internal static List<IDevice> CreateDeviceList(List<TDeviceSettings> deviceSettings) {
            return new List<IDevice> {
                new Dev_GasSensor(deviceSettings[0]),
                new Dev_Controller(deviceSettings[1])
            };
        }

        internal static List<TDeviceSettings> GetDeviceSettings(IAppSettings? appSettings) {
            if (appSettings == null) { throw new NullReferenceException(); }
            var gasSensorSettings = appSettings.DeviceSettings.FirstOrDefault(device => device.DeviceName.Contains("Gas"))
                                    ?? throw new Exception("GasSensor not found in settings! Name mismatch?");
            var controllerSettings = appSettings.DeviceSettings.FirstOrDefault(device => device.DeviceName.Contains("Con") || device.DeviceName.Contains("SPS") || device.DeviceName.Contains("PLC"))
                                    ?? throw new Exception("Controller not found in settings! Name mismatch?");
            return new List<TDeviceSettings> { gasSensorSettings, controllerSettings };
        }

        internal static List<IDevice> GetDeviceList(List<TDeviceSettings>? deviceSetting) {
            if (deviceSetting == null) { throw new NullReferenceException(); }
            List<IDevice> devices = new() {
                new Dev_GasSensor(deviceSetting[0]),
                new Dev_Controller(deviceSetting[1])
            };
            return devices;
        }

        internal static IReactorControl CreateReactorControl(TReactorControlSettings? reactorControlSettings, IDevice? device, IDataHandler? dataStore) {
            return (dataStore == null) || (device == null) || (reactorControlSettings == null)
                ? throw new NullReferenceException()
                : (IReactorControl)new ReactorControl(reactorControlSettings, (msg) => device.SendCommand(msg), dataStore);
        }

        internal static IProcessSimulation CreateProcessSimulation(IAppSettings? appSettings, IDataHandler? dataStore, IReactorControl? reactorControl) {
            return ( dataStore == null ) || ( appSettings == null ) || ( reactorControl == null )
                ? throw new NullReferenceException()
                : (IProcessSimulation) new ProcessSimulation(appSettings, dataStore, reactorControl);
        }

        private static void EnsureLogDirectoryExists(string logDirectory) {
            if (!Directory.Exists(logDirectory)) {
                Directory.CreateDirectory(logDirectory);
            }
        }
    }
}
