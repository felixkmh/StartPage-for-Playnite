using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace LandingPage.Converters
{
    class ThicknessToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Thickness thickness)
            {
                return thickness.Bottom;
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double length && !double.IsNaN(length))
            {
                return new Thickness(length);
            }
            return new Thickness(0);
        }
    }

    class DoubleToThicknessConverter : IValueConverter
    {
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Thickness thickness)
            {
                return thickness.Bottom;
            }
            return 0.0;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double length && !double.IsNaN(length))
            {
                return new Thickness(length);
            }
            return new Thickness(0);
        }
    }
}
