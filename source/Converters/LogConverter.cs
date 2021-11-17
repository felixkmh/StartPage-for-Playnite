using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LandingPage.Converters
{
    class LogConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double v && parameter is string log_base_string && double.TryParse(log_base_string, out var log_base))
            {
                return Math.Log(v, log_base);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double v && parameter is string log_base_string && double.TryParse(log_base_string, out var log_base))
            {
                return Math.Pow(log_base, v);
            }
            return 0;
        }
    }
}
