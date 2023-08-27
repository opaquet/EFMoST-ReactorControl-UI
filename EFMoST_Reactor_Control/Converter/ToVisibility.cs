using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UI.Converter {
    public class BoolToVisibility : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is bool b) {
                return b ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Visible; //default to visible
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return (Visibility)value == Visibility.Visible;
        }
    }

    public class BoolInvertedToVisibility : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is bool b) {
                return b ? Visibility.Collapsed : Visibility.Visible;
            }

            return Visibility.Visible; //default to visible
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return (Visibility)value == Visibility.Collapsed;
        }
    }
}
