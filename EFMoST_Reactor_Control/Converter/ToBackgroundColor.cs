using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace UI.Converter {
    public class ConnectedToBackgroundColor : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value is bool b
                ? !b ? new SolidColorBrush(Color.FromRgb(255, 64, 64)) : (object) new SolidColorBrush(Color.FromRgb(250, 250, 250))
                : new SolidColorBrush(Color.FromRgb(250, 250, 250));
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { return null; }
    }

    public class BoolToBackgroundColor : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value is bool b
                ? b ? new SolidColorBrush(Color.FromRgb(255, 128, 64)) : (object)new SolidColorBrush(Color.FromRgb(250, 250, 250))
                : new SolidColorBrush(Color.FromRgb(250, 250, 250));
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { return null; }
    }

}
