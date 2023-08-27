namespace Core.DataTypes {
    public class TDeviceSettings {
        public TDeviceConnectionSettings ConnectionSettings { get; set; } = new();
        public string DeviceName { get; set; } = string.Empty;
    }
}
