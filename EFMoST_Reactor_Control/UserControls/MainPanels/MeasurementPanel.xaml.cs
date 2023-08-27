using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UI.UserControls.MainPanels
{
    /// <summary>
    /// Interaktionslogik für MeasurementPanel.xaml
    /// </summary>
    public partial class MeasurementPanel : UserControl
    {
        public MeasurementPanel() {
            InitializeComponent();
            Loaded += (_, _) => {
                var parentWindow = Window.GetWindow(this);
                if (parentWindow != null) {
                    parentWindow.Closing += (_, _) => Dispose();
                }
            };
        }

        public void Dispose() {
            XYGraph.Dispose();
        }

        public void Bar_Click(object sender, MouseButtonEventArgs e) {
            Task.Run(() => Console.Beep(2400, 50));
            string Name = ((ValueDisplayBar)sender).Name;
            int Index = int.Parse(Name.Substring(1)) - 1;
            if (Index == XYGraph.SelectedValue) return;
            XYGraph.SelectedValue = Index;
            XYGraph.Update(true);

        }

        public void ButtonAnimation(string tgtColor, string endColor, Border brd, double duration = 0.2) {
            brd.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(tgtColor));
            ColorAnimation animation = new ColorAnimation {
                To = (Color)ColorConverter.ConvertFromString(endColor),
                Duration = new Duration(TimeSpan.FromSeconds(duration))
            };
            brd.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }
    }
}
