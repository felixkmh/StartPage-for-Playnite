using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace LandingPage.Converters
{
    class UriToBitmapImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Uri uri = null;

            if (value is Uri imageUri)
            {
                uri = imageUri;
            }
            else if (value is string uriString && Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out var imageUri2))
            {
                uri = imageUri2;
            }

            if (uri is Uri)
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.DelayCreation;
                bmp.CacheOption = BitmapCacheOption.OnDemand;
                if (parameter is string maxHeightString && int.TryParse(maxHeightString, out var maxHeight))
                {
                    bmp.DecodePixelHeight = maxHeight;
                }
                bmp.UriSource = uri;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
