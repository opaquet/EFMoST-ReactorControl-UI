using Core.DataTypes;
using System.Security.Cryptography;
using ViewModel.ViewModels;

namespace ViewModel
{

    public class MainViewModel : BaseViewModel, IDisposable {


        public event Action<double, string, string>? InitProgress;
        public event Action<string>? InitProgressInc;

        private Core.Manager? _coreManger;
        public ProcessControlDataSource AutomaticControlDataSource { 
            get => _coreManger.settings.ReactorControlSettings.AutomaticProcessControlDataSource;
            set => _coreManger.settings.ReactorControlSettings.AutomaticProcessControlDataSource = value;
        }
        public DeviceViewModel? GasSensor { get; private set; }
        public ValueViewModel? GasSensorStateValue { get; private set; }
        public DeviceViewModel? Controller { get; private set; }
        public IReadOnlyList<ValueViewModel>? MeasurementValues { get; private set; }
        public IReadOnlyList<ValueViewModel>? SetPointValues { get; private set; }
        public IReadOnlyList<ValueViewModel>? AnalogOutValues { get; private set; }
        public IReadOnlyList<ValueViewModel>? GasSensorEnviromentValues { get; private set; }
        public IReadOnlyList<ValueViewModel>? DigitalOutIndicators { get; private set; }
        public IReadOnlyList<ValueViewModel>? AutomaticIndicators { get; private set; }
        public IReadOnlyList<ValueViewModel>? ControlActiveIndicators { get; private set; }
        public IReadOnlyList<ValueViewModel>? AlarmIndicators { get; private set; }
        public IReadOnlyList<ValueViewModel>? ControlSettingValues { get; private set; }
        public ChartViewModel? Chart { get; private set; }
        public ValueViewModel? IndicatorDirectControl { get; private set; }

        public LogStringViewModel? ApplicationLog { get; private set; }

        public SimulationViewModel? Simulation { get; private set; }

        public void ResetTempPID() {
            _coreManger?.reactorControl?.ResetTempPID();
        }

        public void ResetVentillationPID() {
            _coreManger?.reactorControl?.ResetVentillationPID();
        }

        public MainViewModel() {
            // create the manager instance from the core program logic first
            _coreManger = Factory.CreateManagerInstance();
            _coreManger.InitializationProgress += (d,s1,s2) => InitProgress?.Invoke(d,s1,s2);
            _coreManger.InitializationIncProgress += (s1) => InitProgressInc?.Invoke(s1);
        }

        public async Task InitializeAsync() {
            // this might take a while
            await Task.Run(() => 
                _coreManger?.Begin()
            );

            // now creat sub ViewModels
            ApplicationLog = Factory.CreateLogViewModel(_coreManger.appEventLog,16);

            GasSensor = Factory.CreateDeviceViewModel(_coreManger?.devHandler?.Devices?[0], _coreManger?.deviceLogs?[0] );
            Controller = Factory.CreateDeviceViewModel(_coreManger?.devHandler?.Devices?[1], _coreManger?.deviceLogs?[1] );

            GasSensorStateValue = Factory.CreateGasSenoserStatusValueViewModel(_coreManger);

            MeasurementValues = Factory.CreateMeasurementValueViewModels(_coreManger);
            SetPointValues = Factory.CreateSetPointValueViewModels(_coreManger);
            AnalogOutValues = Factory.CreateAnalogOutValueViewModels(_coreManger);
            GasSensorEnviromentValues = Factory.CreateGasSenoserEnviromentValueViewModels(_coreManger);
            DigitalOutIndicators = Factory.CreateDigitalOutIndicatorViewModels(_coreManger);
            AutomaticIndicators = Factory.CreateAutomationIndicatorViewModels(_coreManger);
            ControlActiveIndicators = Factory.CreateControlActiveIndicatorViewModels(_coreManger);
            AlarmIndicators = Factory.CreateAlarmIndicatorViewModels(_coreManger);

            ControlSettingValues = Factory.CreateControlSettingValueViewModels(_coreManger);

            IndicatorDirectControl = Factory.CreateIndicatorDirectControlViewModel(_coreManger);

            Simulation = Factory.CreateSimulationViewModel(_coreManger.processSimulator);

            Chart = Factory.CreateChartViewModel(_coreManger, MeasurementValues);
        }

        public override void Dispose() {
            Factory.UnsubscribeFromAllEvents(_coreManger);

            _coreManger.InitializationProgress -= InitProgress;
            _coreManger.InitializationIncProgress -= InitProgressInc;

            GasSensor?.Dispose();
            Controller?.Dispose();

            foreach (var item in MeasurementValues) { item.Dispose(); }
            foreach (var item in AnalogOutValues) { item.Dispose(); }
            foreach (var item in GasSensorEnviromentValues) { item.Dispose(); }
            foreach (var item in DigitalOutIndicators) { item.Dispose(); }
            foreach (var item in AutomaticIndicators) { item.Dispose(); }
            foreach (var item in ControlActiveIndicators) { item.Dispose(); }
            foreach (var item in AlarmIndicators) { item.Dispose(); }
            GasSensorStateValue?.Dispose();
            IndicatorDirectControl?.Dispose();

            ApplicationLog?.Dispose();

            _coreManger?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
