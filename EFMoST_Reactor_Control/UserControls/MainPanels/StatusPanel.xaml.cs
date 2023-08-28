using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UI.UserControls.MainPanels {
    /// <summary>
    /// Interaktionslogik für StatusPanel.xaml
    /// </summary>
    public partial class StatusPanel : UserControl {
        private ViewModel.MainViewModel? _VM;
        private CancellationTokenSource cts = new();
        private bool isDisposed = false;

        private bool MotorIsRunning { get => (_VM?.DigitalOutIndicators?[8].IsIndicating ?? false) || (_VM?.DigitalOutIndicators?[9].IsIndicating ?? false); }

        public StatusPanel() {
            InitializeComponent();
            DataContextChanged += PanelStatus_DataContextChanged;
            Loaded += (_, _) => {
                var parentWindow = Window.GetWindow(this);
                if (parentWindow != null) {
                    parentWindow.Closing += (_, _) => Dispose();
                }
            };
        }

        private void PanelStatus_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            _VM = DataContext as ViewModel.MainViewModel;
            StartAnimation();
        }

        public void Dispose() {
            cts.Cancel();
            cts.Dispose();
            isDisposed = true;
        }

        private async void StartAnimation() {
            int[] TopVal = { 325, 320, 310, 300, 300, 310, 320, 325 };
            int[] BottomVal = { 265, 270, 280, 290, 290, 280, 270, 265 };
            int[] Width = { 15, 10, 8, 6, 6, 8, 10, 15 };
            int idx = 0;
            while (!isDisposed && !cts.Token.IsCancellationRequested) {
                string pathData = MotorIsRunning
                            ? $"M450,295 A5,{Width[idx]},180,0,0,450,{TopVal[idx]} A5,{Width[idx]},180,0,0,450,295 A5,{Width[idx]},180,0,1,450,{BottomVal[idx]} A5,{Width[idx]},180,0,1,450,295"
                            : "M450,295 A5,15,180,0,0,450,325 A5,15,180,0,0,450,295 A5,15,180,0,1,450,265 A5,15,180,0,1,450,295";
                UpdateUI(() => Rührerblatt.Data = Geometry.Parse(pathData));

                try {
                    await Task.Delay(20, cts.Token);
                } catch (TaskCanceledException) {
                    // Task was canceled, handle it gracefully or just ignore.
                }
                idx++;
                if (idx > 7) {
                    idx = 0;
                    UpdateUI(() => UpdateStatus());
                }
            }
        }

        private void UpdateUI(Action updateAction) {
            if (Dispatcher.CheckAccess()) {
                // We are on the UI thread, execute the action directly
                updateAction();
            } else {
                // We are not on the UI thread, use Dispatcher.Invoke
                Dispatcher.Invoke(updateAction);
            }
        }

        private static SolidColorBrush GetBrush(bool condition) {
            return new SolidColorBrush(condition ? Color.FromRgb(64, 240, 64) : Color.FromRgb(211, 211, 211));
        }


        public void UpdateStatus() {
            if (_VM == null) { return; }
            // Druckluft
            v_air.Fill = GetBrush(_VM.DigitalOutIndicators[1].IsIndicating);
            v_air_prop_center.Fill = GetBrush(_VM.DigitalOutIndicators[3].IsIndicating);
            v_air_prop.Fill = GetBrush(_VM.AnalogOutValues[2].Value > 0);
            l_air.Text = $"{(_VM.SetPointValues[2].Value * (_VM.ControlActiveIndicators[3].IsIndicating ? 1 : 0)):N0} {_VM.MeasurementValues[2].Unit}";
            lr_air.Text = $"{_VM.MeasurementValues[2].Value:N0}  {_VM.MeasurementValues[2].Unit}";

            // Feed
            p_feed.Fill = GetBrush(_VM.AnalogOutValues[1].Value > 0);
            l_feed.Text = $"{(_VM.SetPointValues[3].Value * (_VM.ControlActiveIndicators[3].IsIndicating ? 1 : 0)):N0} {_VM.MeasurementValues[3].Unit}";
            lr_feed.Text = $"{_VM.MeasurementValues[3].Value:N0} {_VM.MeasurementValues[3].Unit}";

            //Wasser
            v_water.Fill = GetBrush(_VM.DigitalOutIndicators[4].IsIndicating);

            //Kühlwasser
            v_coolwater_m.Fill = GetBrush(_VM.DigitalOutIndicators[5].IsIndicating);
            v_coolwater_b.Fill = GetBrush(_VM.DigitalOutIndicators[6].IsIndicating);

            //motor
            motor_prop.Fill = GetBrush(MotorIsRunning);
            l_motor.Text = $"{( _VM.SetPointValues[1].Value * ( MotorIsRunning ? 1 : 0 ) ):N0} {_VM.MeasurementValues[1].Unit}";
            lr_motor.Text = $"{_VM.MeasurementValues[1].ValueString} {_VM.MeasurementValues[1].Unit}";

            //schaum
            p_foam.Fill = GetBrush(_VM.DigitalOutIndicators[10].IsIndicating);

            //Messwerte
            l_p1.Text = $"p = {_VM.MeasurementValues[13].ValueString} {_VM.MeasurementValues[13].Unit}";
            l_Level.Text = $"V = {_VM.MeasurementValues[0].ValueString} {_VM.MeasurementValues[0].Unit}";
            l_Temp.Text = $"T = {_VM.MeasurementValues[4].ValueString} {_VM.MeasurementValues[4].Unit}";
            l_dO2.Text = $"dO2 = {_VM.MeasurementValues[7].ValueString} {_VM.MeasurementValues[7].Unit}";
            l_O2.Text = $"O2 = {_VM.MeasurementValues[8].ValueString} {_VM.MeasurementValues[8].Unit}";
            l_pH.Text = $"pH = {_VM.MeasurementValues[6].ValueString} {_VM.MeasurementValues[6].Unit}";

            //Füllstand
            double V = _VM.MeasurementValues[0].Value;
            V = Math.Min(1200, Math.Max(0, V));
            double L = 290 - V / 1200 * 190;
            pic_LevelP1.StartPoint = new Point(308, L);
            pic_LevelP2.Point = new Point(492, L);

            //Gas Sensor
        
            GasSensor.Fill = _VM.GasSensor.IsConnected ? new SolidColorBrush(Color.FromRgb(64, 240, 64)) : new SolidColorBrush(Color.FromRgb(240, 64, 64));
            if (!_VM.GasSensor.IsConnected) {
                l_gassensor.Text = "State = ?";
                l_gassensor_Temp.Text = "T = ?";
                l_gassensor_Humid.Text = "H = ?";
                l_gassensor_Press.Text = "pa = ?";
            } else {
                l_gassensor.Text = $"State = {_VM.GasSensorStateValue.Value}";
                l_gassensor_Temp.Text = $"T = {_VM.GasSensorEnviromentValues[0].ValueString} {_VM.GasSensorEnviromentValues[0].Unit}";
                l_gassensor_Humid.Text = $"H = {_VM.GasSensorEnviromentValues[1].ValueString} {_VM.GasSensorEnviromentValues[1].Unit}";
                l_gassensor_Press.Text = $"pa = {_VM.GasSensorEnviromentValues[2].ValueString} {_VM.GasSensorEnviromentValues[2].Unit}";
            }

            l_H2S.Text = $"H2S = {_VM.MeasurementValues[15].ValueString} {_VM.MeasurementValues[15].Unit}";
            l_Eth.Text = $"Eth = {_VM.MeasurementValues[14].ValueString} {_VM.MeasurementValues[14].Unit}";

        }

    }
}
