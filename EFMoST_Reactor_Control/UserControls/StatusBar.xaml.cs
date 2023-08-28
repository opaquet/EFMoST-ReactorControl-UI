using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ViewModel;

namespace UI.UserControls {

    public partial class StatusBar : UserControl {
        private CancellationTokenSource cts = new();
        private bool isDisposed = false;

        public StatusBar() {
            InitializeComponent();
            StartClockUpdate();
            DataContextChanged += StatusBar_DataContextChanged;
        }

        private void StatusBar_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (DataContext is not ViewModel.MainViewModel DC) { return; }
            DC.Controller.RX += () => UpdateIndicator(RXControllerIndicator,"#f44");
            DC.GasSensor.RX += () => UpdateIndicator(RXGasSensorIndicator, "#f44");
            DC.Controller.TX += () => UpdateIndicator(TXControllerIndicator, "#4f4");
            DC.GasSensor.TX += () => UpdateIndicator(TXGasSensorIndicator, "#4f4");
        }

        private async void StartClockUpdate() {
            while (!isDisposed && !cts.Token.IsCancellationRequested) {
                try {
                    DateTime Time = DateTime.Now;
                    LabelClockHours.Text = Time.ToString("HH");
                    LabelClockMinutes.Text = Time.ToString("mm");
                    await Task.Delay(1000, cts.Token);
                } catch { }
            }
        }

        public static void RXTXButtonAnimation(string tgtColor, string endColor, Border brd, double duration = 0.2) {
            brd.Background = new SolidColorBrush((Color) ColorConverter.ConvertFromString(tgtColor));
            ColorAnimation animation = new() {
                To = (Color) ColorConverter.ConvertFromString(endColor),
                Duration = new Duration(TimeSpan.FromSeconds(duration))
            };
            brd.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }

        private void UpdateIndicator(Border indicator, string color) {
            Dispatcher.Invoke(() => {
                RXTXButtonAnimation(color, "#d3d3d3", indicator);
            });
        }

        private void CloseButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            Window.GetWindow(this).Close();
        }

        private void GasSensorStatus_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            Task.Run(() => Console.Beep(2400, 50));
            RXTXButtonAnimation("#222", "#f44", (Border)sender);
            MainViewModel DC = (MainViewModel) DataContext;
            DC.GasSensor.ReconnectDevice.Execute(null);
        }

        private void ControllerStatus_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            Task.Run(() => Console.Beep(2400, 50));
            RXTXButtonAnimation("#222", "#f44", (Border) sender);
            MainViewModel DC = (MainViewModel) DataContext;
            DC.Controller.ReconnectDevice.Execute(null);
        }
    }
}
