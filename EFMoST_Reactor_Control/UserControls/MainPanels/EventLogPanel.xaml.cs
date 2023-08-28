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
        MainViewModel? viewModel;
        public EventLogPanel()
        {
            InitializeComponent();
            DataContextChanged += EventLogPanel_DataContextChanged;
        }

        private void EventLogPanel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            viewModel = (MainViewModel) DataContext;
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

        private void port0_sendstr_KeyDown(object sender, KeyEventArgs e) {
            if (viewModel == null) { return; }
            if (e.Key == Key.Enter) {
                viewModel.GasSensor.SendCommandToDevice.Execute(port0_sendstr.Text);
            }
        }

        private void port1_sendstr_KeyDown(object sender, KeyEventArgs e) {
            if (viewModel == null) { return; }
            if (e.Key == Key.Enter) {
                viewModel.Controller.SendCommandToDevice.Execute(port1_sendstr.Text);
            }
        }

        private void Button_Click_0(object sender, RoutedEventArgs e) {
            if (viewModel == null) { return; }
            viewModel.Controller.SendCommandToDevice.Execute(port0_sendstr.Text);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) {
            if (viewModel == null) { return; }
            viewModel.Controller.SendCommandToDevice.Execute(port1_sendstr.Text);
        }


    }
}
