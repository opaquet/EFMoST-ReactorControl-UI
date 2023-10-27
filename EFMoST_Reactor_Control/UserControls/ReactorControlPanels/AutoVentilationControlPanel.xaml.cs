using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using ViewModel;

namespace UI.UserControls.ReactorControlPanels {
    /// <summary>
    /// Interaktionslogik für AutoVentilationControlPanel.xaml
    /// </summary>
    public partial class AutoVentilationControlPanel : UserControl {
        public AutoVentilationControlPanel() {
            InitializeComponent();
        }

        private void DigitalStateIndicator_MouseDown(object sender, MouseButtonEventArgs e) {
            Task.Run(() => Console.Beep(1800, 50));
            try {
                MainViewModel DC = DataContext as MainViewModel;
                DC.ResetVentillationPID();
            } catch { }
        }
    }
}
