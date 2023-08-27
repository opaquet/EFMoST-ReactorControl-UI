using System;
using System.Globalization;
using System.Windows.Data;

namespace UI.Converter {
    public class HeightToFontSize : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value is double size ? Math.Max(6, size * 0.4) : (object)12;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { return null; }
    }
}
