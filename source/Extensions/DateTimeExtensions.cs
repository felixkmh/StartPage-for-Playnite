using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LandingPage
{
    public static class DateTimeExtensions
    {
        public static DateTime RoundToClosestMinute(this DateTime time)
        {
            double seconds = time.TimeOfDay.TotalSeconds % 60;
            if (seconds >= 30)
            {
                return time.AddSeconds(60 - seconds);
            } else
            {
                return time.AddSeconds(-seconds);
            }
        }

        public static string ToGroupName(this DateTime? dateTime)
        {
            IValueConverter converter = ResourceProvider.GetResource<IValueConverter>("DateTimeToLastPlayedConverter");
            return converter.Convert(dateTime, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture) as string;
            //var delta = DateTime.Today.Date - dateTime.Date;
            //if (delta.TotalDays < 1)
            //{
            //    // Today
            //    return $"Today ({dateTime.DayOfWeek})";
            //} else if (delta.TotalDays < 2)
            //{
            //    // yesterday
            //    return "Yesterday";
            //} else if (delta.TotalDays < 7)
            //{
            //    return dateTime.DayOfWeek.ToString();
            //    // this week
            //} else if (delta.TotalDays < 14)
            //{
            //    // more than a week
            //    return "More than a week ago";
            //} else
            //{
            //    return "More than 2 weeks ago";
            //}
        }
    }
}
