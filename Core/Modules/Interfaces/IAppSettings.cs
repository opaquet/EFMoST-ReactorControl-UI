using Core.DataTypes;


namespace Core.Modules.Interfaces
{
    public interface IAppSettings : IDisposable
    {
        event Action<string, int> LogEvent;
        List<TDeviceSettings> DeviceSettings { get; }
        TReactorControlSettings ReactorControlSettings { get; }
        TSimulationSettings SimSettings { get; }

        void Begin();

        void SaveSettings();
    }
}