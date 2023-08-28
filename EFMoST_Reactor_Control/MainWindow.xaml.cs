using System;
using System.Threading.Tasks;
using System.Windows;
using ViewModel;

namespace EFMoST_Reactor_Control {
    public partial class MainWindow : Window {

        public event Action? CoreInitialized;
        public event Action<double, string, string> CoreInitProgress;
        public event Action<string> CoreInitProgressInc;

        MainViewModel viewModel;

        public MainWindow() {
            InitializeComponent();
            Closing += MainWindow_Closing;
            viewModel = new MainViewModel();
            viewModel.InitProgress += (d,s1,s2) => CoreInitProgress?.Invoke(d,s1,s2);
            viewModel.InitProgressInc += (s1) => CoreInitProgressInc?.Invoke(s1);
        }

        public void Begin() {
            Task.Run(() => { InitializeViewModel(); });
        }


        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e) {
            Task.Run(() => { Console.Beep(1800, 50); Console.Beep(1600, 50); });
            MainViewModel? VM = DataContext as MainViewModel;
            VM?.Dispose();
        }

        private async void InitializeViewModel() {
            await viewModel.InitializeAsync();
            Dispatcher.Invoke(() =>
            {
                DataContext = viewModel;
            });
            CoreInitialized?.Invoke();
        }
    }
}
