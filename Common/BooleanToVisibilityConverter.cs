using System;
using System.Windows;
using System.Windows.Data;

namespace ScoreAlerts.Common
{
    /// <summary>
    /// Value converter that translates true to <see cref="Visibility.Visible"/> and false to
    /// <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public sealed class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value is bool && (bool)value) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is Visibility && (Visibility)value == Visibility.Visible;
        }
    }

    public sealed class BooleanInvertedToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value is bool && !(bool)value) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is Visibility && (Visibility)value == Visibility.Visible;
        }
    }

    public sealed class BooleanToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double disabledAlpha = 0.0;
            if (parameter != null)
                disabledAlpha = double.Parse(parameter.ToString());
            return (value is bool && !(bool)value) ? disabledAlpha : 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is double && (double)value == 1.0;
        }
    }
}
