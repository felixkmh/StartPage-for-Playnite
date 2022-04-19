using LandingPage.Models.SuccessStory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace LandingPage.Converters
{
    class AchievementToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            UriToBitmapImageConverter uriToBitmapImageConverter = new UriToBitmapImageConverter();
            if (value is Achivement achivement && !string.IsNullOrEmpty(achivement.UrlUnlocked))
            {
                var configPath = LandingPageExtension.Instance.PlayniteApi.Paths.ConfigurationPath;
                var iconCachePath = Path.Combine(configPath, "cache", "SuccessStory");
                if (Directory.Exists(iconCachePath))
                {
                    int maxLenght = (achivement.Name.Replace(" ", "").Length >= 10) ? 10 : achivement.Name.Replace(" ", "").Length;
                    var iconFileName = GetFileNameFromAchievement(achivement);
                    iconFileName += "_" + achivement.Name.Replace(" ", "").Substring(0, maxLenght);
                    iconFileName = string.Concat(iconFileName.Split(Path.GetInvalidFileNameChars()));
                    iconFileName += "_Unlocked.png";
                    var iconPath = Path.Combine(iconCachePath, iconFileName);
                    if (File.Exists(iconPath))
                    {
                        return uriToBitmapImageConverter.Convert(iconPath, targetType, parameter, culture);
                    }
                }
                return uriToBitmapImageConverter.Convert(achivement.UriUnlocked, targetType, parameter, culture);
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        // https://github.com/Lacro59/playnite-successstory-plugin/blob/085b836e93334bf3a283f5a5d3b7698ec99d68f1/source/Models/Achievements.cs#L298
        private static string GetFileNameFromAchievement(Achivement achivement)
        {
            string NameFromUrl = string.Empty;
            string url = achivement.UrlUnlocked;
            List<string> urlSplited = url.Split('/').ToList();

            int Length = 5;
            if (url.Length > 10)
            {
                Length = 10;
            }
            if (url.Length > 15)
            {
                Length = 15;
            }

            if (url.IndexOf("epicgames.com") > -1)
            {
                NameFromUrl = "epic_" + achivement.Name.Replace(" ", "") + "_" + url.Substring(url.Length - Length).Replace(".png", string.Empty);
            }

            if (url.IndexOf(".playstation.") > -1)
            {
                NameFromUrl = "playstation_" + achivement.Name.Replace(" ", "") + "_" + url.Substring(url.Length - Length).Replace(".png", string.Empty);
            }

            if (url.IndexOf(".xboxlive.com") > -1)
            {
                NameFromUrl = "xbox_" + achivement.Name.Replace(" ", "") + "_" + url.Substring(url.Length - Length);
            }

            if (url.IndexOf("steamcommunity") > -1)
            {
                NameFromUrl = "steam_" + achivement.ApiName;
                if (urlSplited.Count >= 8)
                {
                    NameFromUrl += "_" + urlSplited[7];
                }
            }

            if (url.IndexOf(".gog.com") > -1)
            {
                NameFromUrl = "gog_" + achivement.ApiName;
            }

            if (url.IndexOf(".ea.com") > -1)
            {
                NameFromUrl = "ea_" + achivement.Name.Replace(" ", "");
            }

            if (url.IndexOf("retroachievements") > -1)
            {
                NameFromUrl = "ra_" + achivement.Name.Replace(" ", "");
            }

            if (url.IndexOf("exophase") > -1)
            {
                NameFromUrl = "exophase_" + achivement.Name.Replace(" ", "");
            }

            if (url.IndexOf("overwatch") > -1)
            {
                NameFromUrl = "overwatch_" + achivement.Name.Replace(" ", "");
            }

            if (url.IndexOf("starcraft2") > -1)
            {
                NameFromUrl = "starcraft2_" + achivement.Name.Replace(" ", "");
            }

            if (!url.Contains("http"))
            {
                return url;
            }

            return NameFromUrl;
        }
    }
}
