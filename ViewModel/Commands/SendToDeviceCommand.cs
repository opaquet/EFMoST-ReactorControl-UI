using System.Windows.Input;

namespace ViewModel.Commands {
    internal class SendToDeviceCommand : ICommand {
        Core.Devices.IDevice _device;

        public SendToDeviceCommand(Core.Devices.IDevice dev) {
            _device = dev;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) {
            return true;
        }

        public void Execute(object? parameter) {
            string? command = (string?) parameter;
            if (!string.IsNullOrEmpty(command)) {
                _device.SendCommand(command);
            } 
        }
    }
}
