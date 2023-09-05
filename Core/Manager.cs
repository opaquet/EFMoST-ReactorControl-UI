using Core.DataTypes;
using Core.Modules.Interfaces;
using System.Text.Json.Nodes;

namespace Core
{
    public class Manager : IDisposable {
        // event for updating the splash screen
        public event Action? InitializationFinished;
        public event Action<double, string, string>? InitializationProgress;
        public event Action<string>? InitializationIncProgress;

        // events to communicate state changes to the viewmodel layer
        public event Action<TComputedValuesData>? ComputedValuesDataUpdate;
        public event Action<TGasSensorData>? GasSensorDataUpdate;
        public event Action<TControllerData>? ControllerDataUpdate;

        // Core Modules
        public IEventLogger? appEventLog;
        public List<IEventLogger>? deviceLogs;
        public IAppSettings? settings;
        public IDeviceHandler? devHandler;
        public IReactorControl? reactorControl;
        public IProcessSimulation? processSimulator;
        public IDataHandler? dataStore;

        public DateTime StartTime { get; } = DateTime.Now;

        public Manager() {

        }

        public void Begin() {
            InitializeAppEventLog(0);
            InitializeSettingsModule(15);
            InitializeDataStorModule(30);
            InitializeDeviceHandlerModule(45);
            // wait for devices to finish their connection attempts (not important if sucessful or not)
            while (( !devHandler?.IsConnectionAttemptFinished[0] ?? false ) && ( !devHandler?.IsConnectionAttemptFinished[1] ?? false )) {
                Thread.Sleep(100);
            }
            InitializeReactorControlAndSimModule(60);




            // just in case any ComPort was changed by the autodetection performed by the devHandler  
            settings?.SaveSettings();

            appEventLog?.Append($"*** Application started! ***", "Manager  ", 0);
            InitializationFinished?.Invoke();
        }

        #region Module Initialization
        private void InitializeAppEventLog(int progress) {
            InitializationProgress?.Invoke(progress, "Application initialization...", "");
            appEventLog = Factory.CreateEventLogger();
            appEventLog.Append($"*** Starting application... ***", "Manager  ", 0);
        }
        private void InitializeSettingsModule(int progress) {
            InitializationProgress?.Invoke(progress, "Loading Settings...", "");
            settings = Factory.CreateAppSettings();
            settings.LogEvent += (msg, logLevel) => appEventLog?.Append($"{msg}", " AppSettings", logLevel);
            settings.Begin();
        }
        private void InitializeDataStorModule(int progress) {
            InitializationProgress?.Invoke(progress, "Initializing DataStore...", "");
            dataStore = Factory.CreateDataHandler();
            dataStore.GasSensorCycleComplete += (data) => appEventLog?.Append($"GasSensor measurement cycle complete! Eth={data.Ethanol:N1} g/L  H2S={data.H2S:N1} ppm", " DataStore", 1);
            dataStore.GasSensorCycleComplete += (data) => ComputedValuesDataUpdate?.Invoke(data);
            dataStore.LogEvent += (msg, logLevel) => appEventLog?.Append($"{msg}", " DataHandler", logLevel);
            dataStore.StartupEvent += InitializationIncProgress;
            dataStore.Begin();
        }
        private void InitializeReactorControlAndSimModule(int progress) {
            InitializationProgress?.Invoke(progress, "Starting Control & Sim Module...", "");
            reactorControl = Factory.CreateReactorControl(settings?.ReactorControlSettings, devHandler?.Devices[1], dataStore);
            processSimulator = Factory.CreateProcessSimulation(settings, dataStore, reactorControl);
            reactorControl.SetSimlation(processSimulator);
            reactorControl.LogEvent += (msg, logLevel) => appEventLog?.Append(msg, " ControlModule", logLevel);
            reactorControl.StartupEvent += InitializationIncProgress;
            reactorControl.Begin();
            processSimulator.LogEvent += (msg, logLevel) => appEventLog?.Append(msg, " SimModule", logLevel);
            processSimulator.StartupEvent += InitializationIncProgress;
            processSimulator.Begin();
        }

        private void InitializeDeviceHandlerModule(int progress) {
            InitializationProgress?.Invoke(progress, "Starting DeviceHandler...", "");
            deviceLogs = Factory.CreateAllEventLoggers();
            devHandler = Factory.CreateDeviceHandler(settings, deviceLogs);
            devHandler.LogEvent += (msg, logLevel) => appEventLog?.Append(msg, " DeviceHandler", logLevel);
            devHandler.NewDataRecieved += DevHandlerNewDataRecieved;
            devHandler.StartupEvent += InitializationIncProgress;
            devHandler.MessageRelay += DevHandlerRelayRecieved;
            devHandler.Begin();
        }
        #endregion

        public void Dispose() {

            if (dataStore != null) { dataStore.StartupEvent -= InitializationIncProgress; }

            if (reactorControl != null) { reactorControl.StartupEvent -= InitializationIncProgress; }
            if (processSimulator != null) { processSimulator.StartupEvent -= InitializationIncProgress; }
            if (devHandler != null) { devHandler.StartupEvent -= InitializationIncProgress; }
            if (devHandler != null) { devHandler.NewDataRecieved -= DevHandlerNewDataRecieved; }
            if (devHandler != null) { devHandler.MessageRelay -= DevHandlerRelayRecieved; }

            appEventLog?.Append($"*** Stopping application... ***", "Manager  ", 0);
            settings?.Dispose();
            devHandler?.Dispose();
            reactorControl?.Dispose();
            processSimulator?.Dispose();
            dataStore?.Dispose();
            appEventLog?.Append($"*** Application stopped! ***", "Manager  ", 0);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///recieve new Data from the Devices of the devHandler and put them into the datsStore
        /// </summary>
        /// <param name="devID">device ID (0=GasSensor, 1=Controller)</param>
        /// <param name="data">recieved Data as a JsonObject</param>
        private void DevHandlerNewDataRecieved(int devID, JsonObject data) {
            try {
                switch (devID) {
                    case 0:
                        dataStore?.AddGassensorData(data);
                        TGasSensorData gasSensorData = dataStore?.GetLastGasSensorDatum() ?? new();
                        GasSensorDataUpdate?.Invoke(gasSensorData);
                        break;
                    case 1:
                        dataStore?.AddControllerData(data);
                        TControllerData controllerData = dataStore?.GetLastControllerDatum() ?? new();
                        ControllerDataUpdate?.Invoke(controllerData);
                        break;
                    default:
                        appEventLog?.Append($"New data error: Invalid devID ({devID})!", "Manager  ", 0);
                        break;
                }
            } catch (Exception e) {
                appEventLog?.Append("New data error: " + e.Message, "Manager  ", 0);
            }
        }

        /// <summary>
        /// recieve raw data (relayed by the PLC) as string and send it to the data store to be saved as is
        /// </summary>
        /// <param name="devID">device ID (can only be 1=Controller since the gassensor does not relay raw data)</param>
        /// <param name="message">recieved raw data as string</param>
        private void DevHandlerRelayRecieved(int devID, string message) {
            try {
                switch (devID) {
                    case 1:
                        dataStore?.AddRawMesurementData(message);
                        break;
                    default:
                        appEventLog?.Append($"New data error: Invalid devID ({devID})!", "Manager  ", 0);
                        break;
                }
            } catch (Exception e) {
                appEventLog?.Append("Message relay error: " + e.Message, "Manager  ", 0);
            }
        }

    }
}
