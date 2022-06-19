using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LandingPage.Models.SuccessStory
{
    public class Achievements : ObservableObject, IEqualityComparer<Achievements>
    {
        private List<Achievement> items = new List<Achievement>();
        private bool haveAchivements;
        private int total;
        private int unlocked;
        private int locked;
        private int progression;
        private Guid id;
        private string name;

        public List<Achievement> Items { get => items; set { items = value; OnPropertyChanged(); } }
        public bool HaveAchivements { get => haveAchivements; set { haveAchivements = value; OnPropertyChanged(); } }
        public int Total { get => total; set { total = value; OnPropertyChanged(); } }
        public int Unlocked { get => unlocked; set { unlocked = value; OnPropertyChanged(); } }
        public int Locked { get => locked; set { locked = value; OnPropertyChanged(); } }
        public int Progression { get => progression; set { progression = value; OnPropertyChanged(); } }
        public Guid Id { get => id; set { id = value; OnPropertyChanged(); } }
        public string Name { get => name; set { name = value; OnPropertyChanged(); } }

        public DateTime LastUnlocked => items.OrderByDescending(i => i.DateUnlocked).Select(i => i.DateUnlocked).FirstOrDefault() ?? default;

        public override bool Equals(object obj)
        {
            if (obj is Achievements achievements)
            {
                return achievements.Id == Id;
            }
            return base.Equals(obj);
        }

        public bool Equals(Achievements x, Achievements y)
        {
            return x.Id == x.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public int GetHashCode(Achievements obj)
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class Achievement : ObservableObject
    {
        private string name;
        private string apiName = string.Empty;
        private string description;
        private string urlUnlocked;
        private string urlLocked;
        private DateTime? dateUnlocked;
        private bool isHidden = false;
        private float percent = 100;
        //private string category = string.Empty;
        //private string parentCategory = string.Empty;

        public string Name { get => name; set { name = value; OnPropertyChanged(); } }
        public string ApiName { get => apiName; set { apiName = value; OnPropertyChanged(); } }
        public string Description { get => description; set { description = value; OnPropertyChanged(); } }
        public string UrlUnlocked { get => urlUnlocked; set { urlUnlocked = value; OnPropertyChanged(); OnPropertyChanged(nameof(UriUnlocked)); } }
        public string UrlLocked { get => urlLocked; set { urlLocked = value; OnPropertyChanged(); } }
        public DateTime? DateUnlocked { get => dateUnlocked ?? DateTime.MinValue; set { dateUnlocked = value; OnPropertyChanged(); } }
        public bool IsHidden { get => isHidden; set { isHidden = value; OnPropertyChanged(); } }
        public float Percent { get => percent; set { percent = value; OnPropertyChanged(); } }
        //public string Category { get => category; set { category = value; OnPropertyChanged(); } }
        //public string ParentCategory { get => parentCategory; set { parentCategory = value; OnPropertyChanged(); } }
        [Newtonsoft.Json.JsonIgnore]
        public Uri UriUnlocked {
            get
            {
                if (this is Achievement achivement && !string.IsNullOrEmpty(achivement.UrlUnlocked))
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

                        iconFileName = Regex.Replace(WebUtility.HtmlDecode(GetSafePathName(iconFileName)), @"[^\u0020-\u007E]", string.Empty);
                        var iconPath = Path.Combine(iconCachePath, iconFileName);
                        if (File.Exists(iconPath) && Uri.TryCreate(iconPath, UriKind.RelativeOrAbsolute, out var localUri))
                        {
                            try
                            {
                                using (var file = File.Open(iconPath, FileMode.Open, FileAccess.Read))
                                {
                                    return localUri;
                                }
                            }
                            catch (Exception ex)
                            {
                                LandingPageExtension.logger.Debug(ex, $"Could not open achievement image at \"{iconPath}\". Falling back to using \"{UrlUnlocked}\".");
                            }
                        }
                    }
                    if (UrlUnlocked.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Uri.TryCreate(UrlUnlocked, UriKind.Absolute, out var onlineUri))
                        {
                            return onlineUri;
                        }
                    }
                }
                return new Uri(@"/StartPagePlugin;component/star.png", UriKind.Relative);
            }
        }

        public static string GetSafePathName(string filename)
        {
            var path = string.Join(" ", filename.Split(Path.GetInvalidFileNameChars()));
            return Regex.Replace(path, @"\s+", " ").Trim();
        }

        // https://github.com/Lacro59/playnite-successstory-plugin/blob/085b836e93334bf3a283f5a5d3b7698ec99d68f1/source/Models/Achievements.cs#L298
        private static string GetFileNameFromAchievement(Achievement achivement)
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
