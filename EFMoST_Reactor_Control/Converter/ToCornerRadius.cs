using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UI.Converter {
    public class SizeToCornerRadius : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value is double size ? new CornerRadius(size / 2) : (object)new CornerRadius(0);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { return null; }
    }
}
