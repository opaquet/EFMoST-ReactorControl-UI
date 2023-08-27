using Core.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Core.Devices {
    class Dev_Controller : BasicDevice {

        public Dev_Controller(TDeviceSettings deviceSettings) : base(connectStr: "!GD\n", startStr: "!CN\n", stopStr: "!DC\n", deviceSettings.ConnectionSettings) {
            Name = deviceSettings.DeviceName;
        }

        protected override bool IsDevStringCorrect(string devString) {
            bool result = devString.Contains("efmost", StringComparison.OrdinalIgnoreCase);
            return result;
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
            if (RecievedData.ContainsKey("D")) {
                IsCmdValid = true;
            }
            if (RecievedData.ContainsKey("btn")) {
                List<int> pressedButton = new();
                for (int i = 0; i < RecievedData["btn"]?.AsArray().Count; i++) {
                    pressedButton.Add(RecievedData["btn"]?[i]?.GetValue<int>() ?? 0);
                }
                InvokeMessage($"Button(s) {string.Join(",",pressedButton)} pressed!");
            }
            if (RecievedData.ContainsKey("Relay")) {
                string message = RecievedData["Relay"]?.GetValue<string>() ?? "String Copy Error";
                InvokeMessageRelay(message);
            }
            if (RecievedData.ContainsKey("M")) {
                _lastRXtime = DateTime.Now;
                InvokeNewDataRecieved(RecievedData);
            }
            return RecievedData;
        }
    }
}
