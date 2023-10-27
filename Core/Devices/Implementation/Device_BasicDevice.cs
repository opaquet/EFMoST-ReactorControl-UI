using Core.DataTypes;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Text.Json.Nodes;

namespace Core.Devices {
    abstract class BasicDevice : IDevice, IDisposable {
        public const int MAX_CONNECTION_RETRIES = 6;
        public const int MAX_COMMAND_SEND_RETRIES = 3;
        public const int MAX_STOP_STRING_SEND_RETRIES = 1;
        public const int TX_TIMEOUT_MS = 2000;
        public const int RX_TIMEOUT_MS = 1000;
        public const int TX_WAIT_DELAY_MS = 75;
        public const int CONNECTION_KEEPALIVE_TIMEOUT_S = 30;

        #region BasicDevice: events
        public event Action<bool>? ConnectFinished;
        public event Action<JsonObject>? NewDataRecieved;
        public event Action<string>? RX, TX;
        public event Action<string>? Error;
        public event Action<string>? Message;
        public event Action<string>? MessageRelay;
        public event Action<bool>? CommandAcknowledged;
        public event Action? ConnectionLost;

        protected void InvokeRX(string Msg) { RX?.Invoke(Msg); }
        protected void InvokeTX(string Msg) { TX?.Invoke(Msg); }
        protected void InvokeConnectFinished(bool result) { ConnectFinished?.Invoke(result); }
        protected void InvokeNewDataRecieved(JsonObject data) { NewDataRecieved?.Invoke(data); }
        protected void InvokeError(string Msg) { Error?.Invoke(Msg); }
        protected void InvokeMessage(string Msg) { Message?.Invoke(Msg); }
        protected void InvokeMessageRelay(string Msg) { MessageRelay?.Invoke(Msg); }
        #endregion

        #region BasicDevice: private vars
        private bool _isConnected = false;
        private SerialPort? _port;
        private ConcurrentQueue<string> _sendStrings = new();
        private CancellationTokenSource _source = new();
        private SemaphoreSlim _semaphore = new(0);
        private string _connectStr;
        private string _startStr;
        private string _stopStr;
        protected DateTime _lastRXtime = DateTime.Now;
        private Timer? _keepAliveTimer;
        private object _lockObject = new object();
        #endregion

        #region BasicDevice: public properties 
        public TDeviceConnectionSettings ComPortSettings { get; set; } = new TDeviceConnectionSettings { PortName = "None", BaudRate = 115200 };
        public bool IsConnected { get => _isConnected; }
        public string DevString { get; protected set; } = "none";
        public string VerString { get; protected set; } = "none";
        public string Name { get; protected set; } = "unknown";
        public bool IsCmdValid { get; protected set; } = false;
        public string ConnectionName { get => $"Serial Port {ComPortSettings.PortName}"; }
        public string PortName { get => ComPortSettings.PortName; }

        #endregion

        public BasicDevice(string connectStr, string startStr, string stopStr, TDeviceConnectionSettings comPortSettings) {
            ComPortSettings = comPortSettings;
            _connectStr = connectStr;
            _startStr = startStr;
            _stopStr = stopStr;
        }

        public void Dispose() {
            Task disconnectTask = Task.Run(Disconnect);
            _source.Cancel();
            _semaphore.Release();
            _source.Dispose();
            _semaphore.Dispose();
            disconnectTask.Wait(1000);
            Task disposeTask = Task.Run( () => {
                _port?.Dispose();
            });
            disposeTask.Wait(1000);
        }

        public bool TestConnection(string portName) {
            using SerialPort serialPort = new() { PortName = portName, BaudRate = ComPortSettings.BaudRate, Parity = Parity.None, DataBits = 8, StopBits = StopBits.One, ReadTimeout = RX_TIMEOUT_MS, DtrEnable = false };
            try {
                serialPort.Open();
                lock (_lockObject) {
                    serialPort.WriteLine(_stopStr);
                }
                Thread.Sleep(TX_WAIT_DELAY_MS);
                lock (_lockObject) {
                    serialPort.DiscardInBuffer();
                    serialPort.DiscardOutBuffer();
                    serialPort.Write(_connectStr);
                }
                for (int i = 0; i < MAX_CONNECTION_RETRIES; i++) {
                    string reply = string.Empty;
                    lock (_lockObject) {
                        reply = GetReplyFromPort(serialPort);
                    }
                    if (!string.IsNullOrEmpty(reply)) {
                        if (IsDevReplyCorrect(reply)) {
                            return true; // If reply is correct, exit early
                        }
                    }
                }
                return false; // No correct reply received after MAX_CONNECTION_RETRIES
            } catch (Exception ex) {
                InvokeError($"Connection Test Error: {ex.Message}");
                return false;
            } finally {
                try {
                    serialPort.Close();
                    Thread.Sleep(TX_WAIT_DELAY_MS * 2);
                } catch { InvokeError($"Connection Test Error: Unable to close {portName}!"); }
            }
        }

        /// <summary>
        /// <b>Connect</b> trys to establishe connection to the device on the given port. ConnectFinished event is fired afte completion.
        /// </summary>
        public void Connect() => Task.Run(() => ConnectAsync());
        public void Disconnect() => Task.Run(() => DisconnectAsync());

        public async Task ConnectAsync() {
            _isConnected = false;
            InvokeMessage($"Resetting {ComPortSettings.PortName}...");
            await DisconnectAsync();
            InvokeMessage($"Establishing connection on {ComPortSettings.PortName}...");
            _port = new SerialPort { PortName = ComPortSettings.PortName, BaudRate = ComPortSettings.BaudRate, Parity = Parity.None, DataBits = 8, StopBits = StopBits.One, ReadTimeout = RX_TIMEOUT_MS, DtrEnable = false };
            try {
                _port.Open();
            } catch (Exception ex) {
                Error?.Invoke(ex.Message);
                InvokeConnectFinished(false);
                return;
            }
            _ = Task.Run(ConnectTask);
        }

        public async Task DisconnectAsync() {
            if (_port == null) {
                return;
            }

            try {
                IsCmdValid = false;
                for (int i = 0; i < MAX_STOP_STRING_SEND_RETRIES; i++) {
                    SendMessage(_stopStr);
                    await WaitForCommandAcknowledgment();
                    _port.DataReceived -= DataRecieved;
                    if (IsCmdValid) {
                        break;
                    }
                }
                if (_port.IsOpen) {
                    _port.Close();
                }

                _port.Dispose();
                _isConnected = false;
            } catch (Exception ex) { InvokeError($"Unable to close Port -> {ex.Message}"); }
        }

        public void SendCommand(string Msg) {
            if (string.IsNullOrEmpty(Msg)) {
                InvokeError($"Command was empty");
                return; 
            }
            _sendStrings.Enqueue(Msg);
            _semaphore.Release();
        }

        protected async Task ConnectTask() {
            if (_port == null) { InvokeError("Trying to connect, but PortObject is null!"); return; }
            SendMessage(_connectStr);
            for (int i = 0; i < MAX_CONNECTION_RETRIES; i++) {
                string reply = GetReplyFromPort(_port);
                if (!string.IsNullOrEmpty(reply)) { 
                    _isConnected = IsDevReplyCorrect(reply);
                    if (_isConnected) { break; }
                }
            }
            if (_isConnected) {
                SendCommand(_startStr);
                await Task.Delay(TX_WAIT_DELAY_MS);
                _keepAliveTimer?.Dispose();
                _keepAliveTimer = new Timer(ConnectionKeepAliveCallback, null, 0, 1000);
                _port.DataReceived += DataRecieved;
                _ = Task.Run(SendCmdTask);
                InvokeConnectFinished(true);
            } else {
                InvokeConnectFinished(false);
            }
        }

        private bool IsDevReplyCorrect(string reply) {
            try {
                JsonObject data = ParseReplyString(reply);
                if (data.ContainsKey("D") && data.ContainsKey("V")) {
                    string devString = data["D"]?.GetValue<string>() ?? string.Empty;
                    if (IsDevStringCorrect(devString)) {
                        DevString = devString;
                        VerString = data["V"]?.GetValue<string>() ?? string.Empty;
                        return true;
                    } 
                } 
                InvokeMessage($"Invalid reply to DeviceRequest: \"{reply}\"");
            } catch (Exception ex) { InvokeError($"Connection error -> {ex.Message}"); }
            return false;
        }

        private static string TrimReplyToValidJson(string reply) {
            int firstIndex = reply.IndexOf('{');
            int lastIndex = reply.LastIndexOf('}');
            string result = ( firstIndex >= 0 && lastIndex > firstIndex ) ? reply.Substring(firstIndex, lastIndex - firstIndex + 1) : string.Empty;
            return result.Replace("\n", "").Replace("\r", ""); ;
        }

        // Timer Callback:  der einmal pro sekunde überprüft, wie alt der timestamp vom letzten Messwert ist.
        // Wenn dieser älter als 20 Sekunden ist, vermute ich einen Verbindungsabbruch und versuche 
        // neu zu verbinden
        private void ConnectionKeepAliveCallback(object? state) {
            double validDataAge = ( DateTime.Now - _lastRXtime ).TotalSeconds;
            if (validDataAge > CONNECTION_KEEPALIVE_TIMEOUT_S) {
                Error?.Invoke("Connection lost? Trying to reconnect!");
                ConnectionLost?.Invoke();
                _lastRXtime = DateTime.Now;
                Connect();
            }
        }

        private void DataRecieved(object sender, SerialDataReceivedEventArgs e) {
            string reply = GetReplyFromPort(_port);
            if (!string.IsNullOrEmpty(reply)) {
                ParseReplyString(reply); 
            }
        }

        private string GetReplyFromPort(SerialPort? port) {
            try {
                string reply = string.Empty;
                lock (_lockObject) {
                    reply = port?.ReadLine() ?? string.Empty;
                }
                InvokeRX(reply);
                return TrimReplyToValidJson(reply);
            } catch (TimeoutException) {
                InvokeRX("Connection Test Timeout!");
                return string.Empty;
            } catch (Exception ex) {
                Error?.Invoke($"Recieve Error: {ex.Message}");
                return string.Empty;
            }
        }

        private async Task SendCmdTask() {
            while (!_source.IsCancellationRequested) {
                await _semaphore.WaitAsync(); 
                IsCmdValid = false;
                if (!_sendStrings.TryDequeue(out string? msg)) {
                    continue;
                }

                EnsurePortIsOpen();
                if (!IsConnected) continue;
                for (int i = 0; i < MAX_COMMAND_SEND_RETRIES; i++) {
                    await Task.Delay(TX_WAIT_DELAY_MS);
                    SendMessage(msg);
                    await WaitForCommandAcknowledgment();
                    if (IsCmdValid) {
                        break;
                    }
                }
                CommandAcknowledged?.Invoke(IsCmdValid);
                if (!IsCmdValid) {
                    InvokeError($"Send Timeout or command not ackowledged!");
                    if (ReadTimeoutCounter++ >= 3) {
                        _isConnected = false;
                        continue;
                    }

                } else {
                    ReadTimeoutCounter = 0;
                }
            }
        }

        private double ReadTimeoutCounter = 0;

        private void EnsurePortIsOpen() {
            if (_port == null) { InvokeError("Trying to ensure, Comport is open bur Port Object is null!");  return; }
            if (_port.IsOpen) {
                return;
            }

            try {
                _port.Open();
            } catch (Exception ex) {
                InvokeError($"Portstatus Error: {ex.Message}");
                _isConnected = false;
            }
        }

        private void SendMessage(string msg) {
            try {
                lock (_lockObject) {
                    _port?.DiscardInBuffer();
                    _port?.DiscardOutBuffer();
                    _port?.WriteLine(msg);
                }
                InvokeTX(msg);
            } catch (Exception ex) {
                InvokeError($"Send Error: {ex.Message}");
            }
        }

        private async Task WaitForCommandAcknowledgment() {
            int maxIterations = TX_TIMEOUT_MS / TX_WAIT_DELAY_MS;
            for (int j = 0; j < maxIterations && !IsCmdValid; j++) {
                await Task.Delay(TX_WAIT_DELAY_MS);
            }
        }

        protected abstract bool IsDevStringCorrect(string devString);

        protected abstract JsonObject ParseReplyString(string reply);
    }
}
