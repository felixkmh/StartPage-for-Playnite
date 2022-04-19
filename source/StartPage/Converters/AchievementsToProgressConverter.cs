using LandingPage.Models.SuccessStory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LandingPage.Converters
{
    class AchievementsToProgressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Achievements a)
            {
                var n_achievements = a.Items?.Count ?? 1;
                var n_unlocked = a.Items?.Count(achievementa => (!achievementa.DateUnlocked?.Equals(default)) ?? false) ?? 0;
                return n_unlocked / (double)Math.Max(1, n_achievements);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
