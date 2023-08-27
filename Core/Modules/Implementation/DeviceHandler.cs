using Core.Devices;
using Core.Modules.Interfaces;
using System.IO.Ports;
using System.Text.Json.Nodes;

namespace Core.Modules
{

    /// <summary>
    /// <b>DeviceHandler</b> This class is meant to act as an interface between the 
    /// core logic of a program and the connected devices such as sensors and controllers.
    /// It maintains a logHandler for each device and takes care of handling all events from the 
    /// devices such as data reiceved and data sent and adds according log entries.
    /// Also raised Errors from the devices are passed through/relayed to the core logic if "LogEvent" 
    /// is beeing listened to.
    /// When new Data is recieved from any of the devices, this data is also passen on to the core 
    /// logic as a JsonObject attached to the NewDataRecieved event.
    /// </summary>
    public class DeviceHandler : IDeviceHandler {
        public event Action<int>? ConnectFinished;
        public event Action<string, int>? LogEvent;
        public event Action<string>? StartupEvent;
        public event Action<int, JsonObject>? NewDataRecieved;
        public event Action<int, string>? MessageRelay;
        public event Action<int, bool>? CommandAcknowledged;

        private readonly object _lockObject = new();

        /// <summary>
        /// Is a device actually connected or not?
        /// </summary>
        public List<bool> IsConnected { get; private set; } = new List<bool>();

        /// <summary>
        /// Has a device finished its connection attempt? This does not necessarily 
        /// mean that is is connected since the attempt might have failed.
        /// </summary>
        public List<bool> IsConnectionAttemptFinished { get; private set; } = new List<bool>();

        public List<IDevice> Devices { get; private set; }

        public List<IEventLogger> DeviceLoggers { get; private set; }

        public DeviceHandler(List<IDevice> devices, List<IEventLogger> deviceLoggers) {
            if (devices.Count != deviceLoggers.Count) {
                throw new ArgumentException("The number of devices and loggers must match.");
            }
            Devices = devices;
            DeviceLoggers = deviceLoggers;
            foreach (IDevice device in Devices) {
                IsConnected.Add(false);
                IsConnectionAttemptFinished.Add(true);
            }
            for (int i = 0; i < Devices.Count; i++) {
                int localI = i;
                Devices[localI].RX += (m) => DeviceLoggers[localI].Append(m, $"{Devices[localI].PortName} RX");
                Devices[localI].TX += (m) => DeviceLoggers[localI].Append(m, $"{Devices[localI].PortName} TX");
                Devices[localI].Error += (m) => LogEvent?.Invoke($"dev{localI} ({Devices[localI].Name}) Error: {m}", 0);
                Devices[localI].Message += (m) => LogEvent?.Invoke($"dev{localI} ({Devices[localI].Name}) Message: {m}", 1);
                Devices[localI].MessageRelay += (m) => MessageRelay?.Invoke(localI, m);
                Devices[localI].ConnectFinished += (b) => DeviceConnectFinished(localI, b);
                Devices[localI].NewDataRecieved += (data) => NewDataRecieved?.Invoke(localI, data);
                Devices[localI].CommandAcknowledged += (b) => CommandAcknowledged?.Invoke(localI, b);
            }
        }

        /// <summary>
        /// <b>Dispose</b> relays the dispose request to all devices
        /// </summary>
        public void Dispose() {
            LogEvent?.Invoke("DeviceHandler stopped!", 1);
            Devices.ForEach(device => device?.Dispose());
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// <b>Begin</b> tries to establish connections to all given devices. 
        /// A ConnectFinished event is fired for each device that completed its connection attempt.
        /// The reuslt can then be read from IsConnected.
        /// </summary>
        public void Begin() {
            LogEvent?.Invoke("DeviceHandler started!", 1);
            CheckDevicePortNames();
            for (int i = 0; i < Devices.Count; i++)
                {
                Connect(i);
            }
        }

        /// <summary>
        /// <b>CheckDevicePortNames</b> checks if any device has "Auto" as PortName. If so, it will run 
        /// a quick test to search for the device on all available physical ComPorts
        /// </summary>
        public void CheckDevicePortNames() {
            List<string> serialPortsToTest = SerialPort.GetPortNames().ToList();
            LogEvent?.Invoke($"System info: available ComPorts: {string.Join(",", serialPortsToTest)}", 2);
            StartupEvent?.Invoke($"Checking Connectionsettings...");
            for (int devID = 0; devID < Devices.Count; devID++) { 
                BasicDevice device = (BasicDevice)Devices[devID];
                if (device.PortName.Equals("Auto", StringComparison.OrdinalIgnoreCase)) {
                    LogEvent?.Invoke($"dev{devID} ({device.Name}): ComPort Autodetection...", 1);
                    string? foundPort = serialPortsToTest.FirstOrDefault(port => {
                        StartupEvent?.Invoke($"dev{devID} ({device.Name}): ComPort Autodetection ({port})...");
                        return device.TestConnection(port); 
                    });
                    if (foundPort != null) {
                        LogEvent?.Invoke($"dev{devID} ({device.Name}): found on {foundPort}!", 1);
                        device.ComPortSettings.PortName = foundPort;
                        serialPortsToTest.Remove(foundPort);
                    } else {
                        LogEvent?.Invoke($"dev{devID} ({device.Name}): not found!", 1);
                    }
                }
            }
        }

        /// <summary>
        /// <b>Connect</b> tries to establish a connection to a specific devices. 
        /// A ConnectFinished event is fired once its connection attempt completes.
        /// The reuslt can then be read from IsConnected.
        /// </summary>
        /// <param name="devID"></param>
        public void Connect(int devID) {
            if (devID < 0 || devID >= Devices.Count) {
                LogEvent?.Invoke($"Connect Error: Invalid device ID {devID}. Valid IDs range from 0 to {Devices.Count-1}.", 0);
                return;
            }
            if (!IsConnectionAttemptFinished[devID]) {
                return;
            }
            try {
                IsConnectionAttemptFinished[devID] = false;
                if (Devices[devID].PortName == "Auto" || Devices[devID].PortName == "None") {
                    LogEvent?.Invoke($"dev{devID} ({Devices[devID].Name}): Port settings invalid! Not connecting!", 0);
                    IsConnectionAttemptFinished[devID] = true;
                } else {
                    StartupEvent?.Invoke($"dev{devID} ({Devices[devID].Name}): connecting...");
                    Devices[devID].Connect();
                }
            } catch (Exception e) { LogEvent?.Invoke($"Connect Error: {e.Message}", 0); }
        }

        /// <summary>
        /// <b>SendCmd</b> tires to send a command string to a given device. 
        /// This happens usually (depending on the device implementation) asynchronously.
        /// A CommandAcknowledged event is fired once finished with a bool that states if the command 
        /// was recieved sucessfully.
        /// However: Not all devices will aknowledge every command, so even a "false" result does not 
        /// necessarily mean that the command was not recieved.
        /// </summary>
        /// <param name="devID"></param>
        /// <param name="cmd"></param>
        public void SendCmd(int devID, string cmd) {
            if (devID < 0 || devID >= Devices.Count) {
                LogEvent?.Invoke($"Send Error: Invalid device ID {devID}. Valid IDs range from 0 to {Devices.Count - 1}", 0);
                return;
            }
            try {
                Devices[devID].SendCommand(cmd);
            } catch (Exception e) { LogEvent?.Invoke($"Send Error: {e.Message}", 0); }
        } 

        private void DeviceConnectFinished(int devID, bool result) {
            if (devID < 0 || devID >= Devices.Count) {
                LogEvent?.Invoke($"Connect finished error: Invalid device ID {devID}. Valid IDs range from 0 to {Devices.Count - 1}", 0);
                return;
            }
            lock (_lockObject) {
                IsConnectionAttemptFinished[devID] = true;
                IsConnected[devID] = result;
            }
            LogEvent?.Invoke($"dev{devID} ({Devices[devID].Name}): Info: DeviceName=\"{Devices[devID].DevString}\", Version={Devices[devID].VerString}", 2);
            LogEvent?.Invoke($"dev{devID} ({Devices[devID].Name}): Connection {(result ? "established!" : "failed!")}", 0);
            ConnectFinished?.Invoke(devID);
        }
    }
}
