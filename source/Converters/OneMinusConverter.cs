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
    class OneMinusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double val)
            {
                if (parameter is string scalarString && double.TryParse(scalarString, out var scalar))
                {
                    return (1 - val) * scalar;
                }
                return 1 - val;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double val)
            {
                return 1 - val;
            }
            return DependencyProperty.UnsetValue;
        }
    }
}
