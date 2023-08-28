using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UI.UserControls {
    /// <summary>
    /// Interaktionslogik für ValueDisplayBar.xaml
    /// </summary>
    public partial class ValueDisplayBar : UserControl {

        public static readonly DependencyProperty WertProperty = DependencyProperty.Register(
            "SetPointBarHeight", typeof(double), typeof(ValueDisplayBar), new PropertyMetadata(default(double)));

        public double Wert {
            get { return (double) GetValue(WertProperty); }
            set { SetValue(WertProperty, value); }
        }

        public static readonly DependencyProperty SollwertProperty = DependencyProperty.Register(
            "SetPoint", typeof(double), typeof(ValueDisplayBar), new PropertyMetadata(default(double)));

        public double Sollwert {
            get { return (double) GetValue(SollwertProperty); }
            set { SetValue(SollwertProperty, value); }
        }

        public static readonly DependencyProperty BackgroundColorProperty = DependencyProperty.Register(
            "BGColor", typeof(Brush), typeof(ValueDisplayBar), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(250, 250, 250))));

        public Brush BackgroundColor {
            get { return (Brush) GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }



        public double SetPoint { get; set; } = 0;
        public double SetPointBarHeight { get; set; } = 0;
        public Brush BGColor { get; set; } = new SolidColorBrush(Color.FromRgb(250,250,250));


        public ValueDisplayBar() {
            InitializeComponent();
        }
    }
}
