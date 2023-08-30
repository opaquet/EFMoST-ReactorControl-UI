using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ViewModel.ViewModels;

namespace UI.UserControls {
    /// <summary>
    /// Interaktionslogik für UpDownButton.xaml
    /// </summary>
    public partial class UpDownButton  {
        private ValueViewModel? viewModel;

        public UpDownButton() {
            InitializeComponent();
            DataContextChanged += UpDownButton_DataContextChanged;
        }


        public void Dispose() {
            DataContextChanged -= UpDownButton_DataContextChanged;
        }


        private void UpDownButton_DataContextChanged(object? sender, DependencyPropertyChangedEventArgs e) {
            if (DataContext == null) { return; }
            try {
                viewModel = (ValueViewModel) DataContext;
            } catch { viewModel = null; }
        }

        private void Btn_Inc_MouseDown(object sender, MouseButtonEventArgs e) {
            if (viewModel == null) { return; }  
            Task.Run(() => Console.Beep(2400, 50));
            viewModel?.Increase();
        }

        private void Btn_Dec_MouseDown(object sender, MouseButtonEventArgs e) {
            if (viewModel == null) { return; }
            Task.Run(() => Console.Beep(1800, 50));
            viewModel?.Decrease();
        }
    }
}
