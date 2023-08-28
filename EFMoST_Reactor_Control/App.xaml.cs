using System;
using System.Windows;
using UI;
using ViewModel;

namespace EFMoST_Reactor_Control {
    public partial class App : Application {
        public App() {

        }

        private SplashWindow splashScreen;
        private MainWindow mainWindow;

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            // Splashscreen anzeigen
            splashScreen = new SplashWindow();
            splashScreen.Show();

            // Hauptfenster erstellen, aber noch nicht anzeigen
            mainWindow = new MainWindow();
            mainWindow.CoreInitProgress += MainWindow_CoreInitProgress;
            mainWindow.CoreInitialized += MainWindow_CoreInitialized;
            mainWindow.CoreInitProgressInc += MainWindow_CoreInitProgressInc;

            mainWindow.Begin();

        }

        private void MainWindow_CoreInitProgressInc(string obj) {
            splashScreen.ProgressInc(obj);
        }

        private void MainWindow_CoreInitialized() {
            Dispatcher.Invoke(() => {
                splashScreen.Close();
                mainWindow.Activate();
                mainWindow.Show();
            });
        }

        private void MainWindow_CoreInitProgress(double arg1, string arg2, string arg3) {
            splashScreen.Progress(arg1, arg2, arg3);
        }
    }
}
