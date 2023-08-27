using Core.DataTypes;
using System;
using System.Text.Json.Nodes;

namespace Core.Devices {
    class Dev_GasSensor : BasicDevice {
        public Dev_GasSensor(TDeviceSettings deviceSettings) : base(connectStr: "r,0,1\n", startStr: "s,0\n", stopStr: "s,-1\n", deviceSettings.ConnectionSettings) {
            Name = deviceSettings.DeviceName;
        }

        protected override bool IsDevStringCorrect(string devString) {
            return devString.Contains("gas", StringComparison.OrdinalIgnoreCase);
        }

        protected override JsonObject ParseReplyString(string reply) {
            JsonObject RecievedData;
            try {
                RecievedData = JsonNode.Parse(reply)?.AsObject() ?? new();
            } catch (Exception ex) {
                InvokeError($"Parse error of \"{reply}\": " + ex.Message);
                return new();
            }
            if (RecievedData.ContainsKey("Cmd")) {
                IsCmdValid = RecievedData["Cmd"]?.GetValue<int>() == 1;
            }
            if (RecievedData.ContainsKey("Sen")) {
                _lastRXtime = DateTime.Now;
                InvokeNewDataRecieved(RecievedData);
            }
            return RecievedData;
        }
    }
}
