using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace UI.UserControls {
    /// <summary>
    /// Interaktionslogik für PanelControl.xaml
    /// </summary>
    public partial class PanelControl : UserControl {
        private int _selectedTab = 0;
        private readonly SolidColorBrush _selectedFontColor = new(Color.FromRgb(255, 255, 255));
        private readonly LinearGradientBrush _unselectedBGColor = new(Color.FromRgb(192, 192, 192), Color.FromRgb(176, 176, 176), 90);
        private readonly SolidColorBrush _unselectedFontColor = new(Color.FromRgb(0, 0, 0));
        private DropShadowEffect _shadowEffect = new();
        private DropShadowEffect _shadowEffectPressed = new();
        private Border[] _buttons;
        private Border[] _panels;
        private TextBlock[] _buttonLabels;

        public PanelControl() {
            InitializeComponent(); // Dies sollte zuerst aufgerufen werden
            _buttons = new Border[] { b0, b1, b2, b3, b4, b5 };
            _buttonLabels = new TextBlock[] { t0, t1, t2, t3, t4, t5 };
            _panels = new Border[] { p0, p1, p2, p3, p4, p5 };
            _shadowEffectPressed.ShadowDepth = 1;
        }

        public static void Dispose() {

        }

        private void Btn_MouseDown(object? sender, MouseButtonEventArgs e) {
            Task.Run(() => Console.Beep(1800, 50));
            _buttons[_selectedTab].Background = _unselectedBGColor;
            _buttons[_selectedTab].Effect = _shadowEffect;
            _buttonLabels[_selectedTab].Foreground = _unselectedFontColor;
            _buttonLabels[_selectedTab].FontWeight = FontWeights.Normal;
            _buttonLabels[_selectedTab].Effect = null;
            _panels[_selectedTab].Visibility = Visibility.Collapsed;

            if (sender is Border border && !string.IsNullOrEmpty(border.Name)) {
                _selectedTab = int.Parse(border.Name[1..]);
            }

            ButtonAnimation("#222", "#88f", _buttons[_selectedTab]);
            //_buttons[_selectedTab].Background = _selectedBGColor;
            _buttons[_selectedTab].Effect = _shadowEffectPressed;
            _buttonLabels[_selectedTab].Foreground = _selectedFontColor;
            _buttonLabels[_selectedTab].FontWeight = FontWeights.Bold;
            _buttonLabels[_selectedTab].Effect = _shadowEffect;
            _panels[_selectedTab].Visibility = Visibility.Visible;
        }

        public static void ButtonAnimation(string tgtColor, string endColor, Border brd, double duration = 0.4) {
            brd.Background = new SolidColorBrush((Color) ColorConverter.ConvertFromString(tgtColor));
            ColorAnimation animation = new() {
                To = (Color) ColorConverter.ConvertFromString(endColor),
                Duration = new Duration(TimeSpan.FromSeconds(duration))
            };
            brd.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }
    }
}
