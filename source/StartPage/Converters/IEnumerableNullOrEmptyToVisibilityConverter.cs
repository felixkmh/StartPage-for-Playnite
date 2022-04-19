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
    class IEnumerableNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string invert && invert == "inverted")
            {
                if (value is System.Collections.IEnumerable enumerable)
                {
                    if (enumerable.GetEnumerator().MoveNext())
                    {
                        return Visibility.Collapsed;
                    }
                }
                return Visibility.Visible;
            } else
            {
                if (value is System.Collections.IEnumerable enumerable)
                {
                    if (enumerable.GetEnumerator().MoveNext())
                    {
                        return Visibility.Visible;
                    }
                }
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
