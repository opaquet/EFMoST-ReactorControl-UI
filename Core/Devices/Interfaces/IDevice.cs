using System.Text.Json.Nodes;

namespace Core.Devices {

    public interface IDevice : IDisposable {
        event Action<JsonObject> NewDataRecieved;
        event Action<string> RX, TX;
        event Action<string> Error;
        event Action<string> Message;
        event Action<string> MessageRelay;
        event Action<bool> ConnectFinished;
        event Action<bool> CommandAcknowledged;
        event Action ConnectionLost;
        bool IsConnected { get; }
        bool IsCmdValid { get; } 
        string Name { get; }
        string DevString { get; }
        string VerString { get; }
        string ConnectionName { get; }
        string PortName { get; }
        bool TestConnection(string portName);
        void Connect();
        void Disconnect();
        void SendCommand(String Msg);
    }


}
