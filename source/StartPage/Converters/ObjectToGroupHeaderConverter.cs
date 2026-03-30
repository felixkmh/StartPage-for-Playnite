using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

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
                int nFull = (int)score / 2;
                int nHalf = (int)score % 2 == 0 ? 0 : 1;
                var sb = new StringBuilder();
                sb.Append(new string('★', nFull));
                sb.Append(nHalf == 1 ? "⯨" : "");
                return sb.ToString();
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
