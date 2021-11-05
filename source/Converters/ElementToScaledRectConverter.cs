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
    class ElementToScaledRectConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is double width && values[1] is double height && parameter is double scale)
            {
                var newWidth = scale * width + 1.5 * 7;
                var newHeight = scale * height;
                var x = (width - newWidth) / 2;
                var y = (height - newHeight) / 2 - (204.75 - newHeight) / 2;
                return new Rect(x, y, newWidth, newHeight);
            }
            return new Rect();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
