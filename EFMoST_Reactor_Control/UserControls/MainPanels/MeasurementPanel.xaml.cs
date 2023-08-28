using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace UI.UserControls.MainPanels {
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
            ValueDisplayBar displayBar = (ValueDisplayBar) sender;
            ButtonAnimation("#202020", "#fafafa", displayBar);
            string Name = displayBar.Name;
            int Index = int.Parse(Name[1..]) - 1;
            if (Index == XYGraph.SelectedValue) { return; }
            XYGraph.SetSelectedGraph(Index);
            XYGraph.Update(true);
        }

        public static void ButtonAnimation(string tgtColor, string endColor, ValueDisplayBar brd, double duration = 0.2) {
            brd.BackgroundColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(tgtColor));
            ColorAnimation animation = new() {
                To = (Color)ColorConverter.ConvertFromString(endColor),
                Duration = new Duration(TimeSpan.FromSeconds(duration))
            };
            brd.BackgroundColor.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }
    }
}
