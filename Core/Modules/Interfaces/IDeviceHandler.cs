using Core.Devices;
using System.Text.Json.Nodes;

namespace Core.Modules.Interfaces
{
    public interface IDeviceHandler : IDisposable
    {
        event Action<int> ConnectFinished;
        event Action<string, int> LogEvent;
        event Action<string> StartupEvent;
        event Action<int, JsonObject> NewDataRecieved;
        event Action<int, string> MessageRelay;
        event Action<int, bool> CommandAcknowledged;

        List<IDevice> Devices { get; }
        List<IEventLogger> DeviceLoggers { get; }

        List<bool> IsConnected { get; }
        List<bool> IsConnectionAttemptFinished { get; }

        void Begin();
        void Connect(int deviceId);
        void SendCmd(int devID, string cmd);
    }
}