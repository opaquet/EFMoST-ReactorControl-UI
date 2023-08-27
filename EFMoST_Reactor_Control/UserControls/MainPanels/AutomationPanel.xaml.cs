using Core.DataTypes;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ViewModel;

namespace UI.UserControls.MainPanels {
    /// <summary>
    /// Interaktionslogik für AutomationPanel.xaml
    /// </summary>
    public partial class AutomationPanel : UserControl
    {
        private MainViewModel? appViewModel;

        public AutomationPanel() {
            InitializeComponent();
            DataContextChanged += PanelAutomation_DataContextChanged;
            Loaded += (_, _) => {
                var parentWindow = Window.GetWindow(this);
                if (parentWindow != null) {
                    parentWindow.Closing += (_, _) => Dispose();
                }
            };
        }

        private void PanelAutomation_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            appViewModel = DataContext as MainViewModel;
            if (appViewModel == null) { return; }
            switch (appViewModel.AutomaticControlDataSource) {
                case ProcessControlDataSource.Measurement:
                    BtnMessung.Background = new SolidColorBrush(Color.FromRgb(255, 128, 64));
                    BtnSimulation.Background = new SolidColorBrush(Color.FromRgb(192, 192, 192));
                    BtnMessungShadow.ShadowDepth = 0;
                    BtnSimualtionShadow.ShadowDepth = 4;
                    break;
                case ProcessControlDataSource.Simulation:
                    BtnMessung.Background = new SolidColorBrush(Color.FromRgb(192, 192, 192));
                    BtnSimulation.Background = new SolidColorBrush(Color.FromRgb(255, 128, 64));
                    BtnMessungShadow.ShadowDepth = 4;
                    BtnSimualtionShadow.ShadowDepth = 0;
                    break;
            }
        }

        public void Dispose() {

        }

        private void Border_MouseDown_Messung(object sender, MouseButtonEventArgs e) {
            if (appViewModel == null) { return; }
            PlayBeep();
            BtnMessung.Background = new SolidColorBrush(Color.FromRgb(255, 128, 64));
            BtnSimulation.Background = new SolidColorBrush(Color.FromRgb(192, 192, 192));
            BtnMessungShadow.ShadowDepth = 0;
            BtnSimualtionShadow.ShadowDepth = 4;
            appViewModel.AutomaticControlDataSource = ProcessControlDataSource.Measurement;
        }

        private void Border_MouseDown_Simulation(object sender, MouseButtonEventArgs e) {
            if (appViewModel == null) { return; }
            PlayBeep();
            BtnMessung.Background = new SolidColorBrush(Color.FromRgb(192, 192, 192));
            BtnSimulation.Background = new SolidColorBrush(Color.FromRgb(255, 128, 64));
            BtnMessungShadow.ShadowDepth = 4;
            BtnSimualtionShadow.ShadowDepth = 0;
            appViewModel.AutomaticControlDataSource = ProcessControlDataSource.Simulation;
        }

        private void PlayBeep() {
            Task.Run(() => Console.Beep(2400, 50));
        }
    }
}
