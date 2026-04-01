using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace LandingPage.Converters
{
    class ObjectToGroupHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                return ResourceProvider.GetResource<IValueConverter>("DateTimeToLastPlayedConverter").Convert(value, targetType, parameter, culture);
            }
            if (value is ScoreGroup scoreGroup)
            {
                var score = scoreGroup == ScoreGroup.None ? 0 : (int)scoreGroup + 1;
                var text = new TextBlock() { FontFamily = new FontFamily("Segoe UI Symbol"), Margin = new System.Windows.Thickness(2) };
                const char empty = '\u2606'; // ☆
                const char half = '\u272E';  // ✮
                const char full = '\u2605'; // ★
                int nFull = (int)score / 2;
                int nHalf = (int)score % 2 == 0 ? 0 : 1;
                int nEmpty = 5 - nFull - nHalf;
                var sb = new StringBuilder();
                sb.Append(new string(full, nFull));
                if (nHalf > 0)
                {
                    sb.Append(half);
                }
                sb.Append(new string(empty, nEmpty));
                text.Text = sb.ToString();
                return text;
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
