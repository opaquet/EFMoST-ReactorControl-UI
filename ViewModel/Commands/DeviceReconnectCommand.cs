using System.Windows.Input;

namespace ViewModel.Commands {
    internal class DeviceReconnectCommand : ICommand {
        Core.Devices.IDevice _device;
        public DeviceReconnectCommand(Core.Devices.IDevice dev) {
            _device = dev;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) {
            return true;
        }

        public void Execute(object? parameter) {
            _device.Connect();
        }
    }
}
