using Core.Devices;
using System.ComponentModel;
using System.Windows.Input;
using ViewModel.Commands;

namespace ViewModel.ViewModels
{
    public class DeviceViewModel : BaseViewModel, INotifyPropertyChanged, IDisposable {

        private readonly IDevice _device;

        private readonly Action<string> _rxHandler;
        private readonly Action<string> _txHandler;
        private readonly Action<bool> _ackHandler;
        private readonly Action<bool> _connectFinishedHandler;
        private readonly Action _connectionLostHandler;

        public bool IsConnected { get; private set; }
        public string ConnectionStatus { 
            get => IsConnected ? "Verbunden!" : "Nicht Verbunden"; 
        }
        public string ConnectionStatusColor { 
            get => IsConnected ? "#fafafa" : "#f44"; 
        }
        public LogStringViewModel Log { get; private set; }
        public ICommand SendCommandToDevice { get; private set; }
        public ICommand ReconnectDevice { get; private set; }

        public event Action? RX, TX;
        public event Action<bool>? ACK;



        public DeviceViewModel(IDevice device, LogStringViewModel log) {
            Log = log;
            _device = device;

            // Store event handlers
            _rxHandler = (msg) => RX?.Invoke();
            _txHandler = (msg) => TX?.Invoke();
            _ackHandler = (b) => ACK?.Invoke(b);
            _connectFinishedHandler = (b) => {
                if (IsConnected != b) { IsConnected = b; } 
            };
            _connectionLostHandler = () => { if (IsConnected) { IsConnected = false; } };


            //event relay
            device.RX += _rxHandler;
            device.TX += _txHandler;
            device.CommandAcknowledged += _ackHandler;
            device.ConnectFinished += (b) => _connectFinishedHandler?.Invoke(b);
            device.ConnectionLost += _connectionLostHandler;

            //commands
            SendCommandToDevice = new SendToDeviceCommand(device);
            ReconnectDevice = new DeviceReconnectCommand(device);

            //device events to update propertys
            device.ConnectFinished += _connectFinishedHandler;
            device.ConnectionLost += _connectionLostHandler;

            IsConnected = device.IsConnected;
        }

        public override void Dispose() {
            if (_device == null) { return; }
            _device.RX -= _rxHandler;
            _device.TX -= _txHandler;
            _device.CommandAcknowledged -= _ackHandler;
            _device.ConnectFinished -= _connectFinishedHandler;
            _device.ConnectionLost -= _connectionLostHandler;
            Log.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
