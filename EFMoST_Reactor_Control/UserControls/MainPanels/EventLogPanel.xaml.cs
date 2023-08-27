using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ViewModel;

namespace UI.UserControls.MainPanels {
    /// <summary>
    /// Interaktionslogik für EventLogPanel.xaml
    /// </summary>
    public partial class EventLogPanel : UserControl
    {
        public EventLogPanel()
        {
            InitializeComponent();
        }

        private void MainLog_TextChanged(object sender, TextChangedEventArgs e) {
            MainLog.ScrollToEnd();
        }

        private void GasSensor_TextChanged(object sender, TextChangedEventArgs e) {
            GasSensorLog.ScrollToEnd();
        }

        private void Controller_TextChanged(object sender, TextChangedEventArgs e) {
            ControllerLog.ScrollToEnd();
        }
    }
}
