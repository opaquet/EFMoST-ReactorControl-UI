using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace UI {
    public partial class SplashWindow : Window {
        public SplashWindow() {
            InitializeComponent();
        }

        public void Progress(double value, string message, string detail_message = "") {
            value = Math.Min(Math.Max(value, 0), 100);
            for (int i = 0; i < 2; i++) {
                Dispatcher.Invoke(() => {  //nicht schön, aber für nen splash screen passt das schon...
                    ProgressBar.Value = value;
                    ProgressLabel.Text = message;
                    ProgressLabelSmall.Text = detail_message;
                }, DispatcherPriority.Render);
            }
            Thread.Sleep(100);
        }

        public void ProgressInc(string detail_message = "") {
            Dispatcher.Invoke(() => {
                ProgressBar.Value++;
                ProgressLabelSmall.Text = detail_message;
            }, DispatcherPriority.Render);
            Dispatcher.Invoke(() => {
                ProgressLabelSmall.Text = detail_message;
            }, DispatcherPriority.Render);
            Thread.Sleep(100);
        }
    }
}
