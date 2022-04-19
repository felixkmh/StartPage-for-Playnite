using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace LandingPage.Converters
{
    class DatabaseIdToUriConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string dataBasePath)
            {
                var path = LandingPageExtension.Instance.PlayniteApi.Database.GetFullFilePath(dataBasePath);
                if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var uri))
                {
                    return uri;
                }
            }

            if (Application.Current.Resources["DefaultGameCover"] is BitmapImage image)
            {
                return image.UriSource;
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
