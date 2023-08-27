using System.Windows;

namespace EFMoST_Reactor_Control {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e) {
            ViewModel.MainViewModel? VM = DataContext as ViewModel.MainViewModel;
            VM?.Dispose();
        }
    }
}
