using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ViewModel;
using ViewModel.ViewModels;

namespace UI.UserControls.MainPanels {
    /// <summary>
    /// Interaktionslogik für DirectControlPanel.xaml
    /// </summary>
    public partial class DirectControlPanel : UserControl
    {
        MainViewModel? DC;
        private bool DirectControl { get => DC?.IndicatorDirectControl.Value == 2; }

        public DirectControlPanel() {
            InitializeComponent();
            Loaded += (_, _) => {
                var parentWindow = Window.GetWindow(this);
                if (parentWindow != null) {
                    parentWindow.Closing += (_, _) => Dispose();
                }
            };
        }

        public void Dispose() {

        }

        private void DCTRL_Click(object sender, MouseButtonEventArgs e) {
            InitializeDataContext();
            if (DC == null) { return; }
            PlayBeep();
            ValueViewModel VM = DC.IndicatorDirectControl;
            DC.Controller.SendCommandToDevice.Execute($"!DR({(DirectControl ? 0 : 1)})\n");
            VM.SetValue(1);
        }

        private void D_Button_Click(object sender, MouseButtonEventArgs e) {
            InitializeDataContext();
            if (DC == null) { return; }
            if (!DirectControl) { return; }
            PlayBeep();
            string Name = ((DigitalStateIndicator) sender).Name;
            int i = int.Parse(Name[5..]) - 1;
            ValueViewModel VM = DC.DigitalOutIndicators[i];
            DC.Controller.SendCommandToDevice.Execute($"!DS(0,{i},{(DC.DigitalOutIndicators[i].IsIndicating ? 0 : 1)})\n");
            VM.SetValue(1);
        }

        private void Slider_DragCompleted(object sender, DragCompletedEventArgs e) {
            InitializeDataContext();
            if (DC == null) { return; }
            if (!DirectControl) { return; }
            PlayBeep();
            string Name = ((Slider)sender).Name;
            int i = int.Parse(Name[6..]) - 1;
            ValueViewModel VM = DC.AnalogOutValues[i];
            DC.Controller.SendCommandToDevice.Execute($"!DS(1,{i},{((Slider)sender).Value:F0})\n");
            VM.SetValue(1);
        }

        private void InitializeDataContext() {
            DC = DataContext as MainViewModel;
        }

        private static void PlayBeep() {
            Task.Run(() => Console.Beep(2400, 50));
        }
    }
}
