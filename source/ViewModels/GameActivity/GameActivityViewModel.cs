using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using LandingPage.Models.GameActivity;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace LandingPage.ViewModels.GameActivity
{
    public class GameActivityViewModel : ObservableObject
    {
        internal IPlayniteAPI playniteAPI;
        internal FileSystemWatcher activityWatcher;
        internal LandingPageSettingsViewModel landingPageSettingsViewModel;

        public ObservableCollection<Activity> Activities { get; set; } = new ObservableCollection<Activity>();

        public Dictionary<GameSource, ulong> PlaytimePerSource => Activities
            .SelectMany(activity => activity.Items)
            .GroupBy(session => session.SourceID)
            .Select(g => new { SourceId = g.First().SourceID, Playtime = g.Sum(s => (long)s.ElapsedSeconds) })
            .ToDictionary(item => playniteAPI.Database.Sources.Get(item.SourceId) ?? new GameSource("Playnite"), item => (ulong)item.Playtime);

        public class DayPlaytime
        {
            public DateTime Day { get; set; } = default;
            public string DayString => Day.ToString("ddd");
            public ulong Playtime { get; set; } = 0;
            public double Filled { get; set; }
            public double OneMinusFilled => 1 - Filled;
        }

        public ObservableCollection<PlayTimePerSource> WeeklyPlaytime { get; set; } = new ObservableCollection<PlayTimePerSource>();


        public ulong PlaytimeLastWeekMax => PlaytimeLastWeek.Max(dpt => dpt.Playtime);

        public ulong TotalPlaytimeThisWeek => (ulong)PlaytimeLastWeek.Sum(a => (long)a.Playtime);

        public List<DayPlaytime> PlaytimeLastWeek
        {
            get
            {
                var lastSevenDays = new[] { 6, 5, 4, 3, 2, 1, 0 }.Select(i => DateTime.Today.AddDays(-i).Date);
                var summed = lastSevenDays.Select(day => new DayPlaytime { Day = day, Playtime = (ulong)Activities.SelectMany(a => a.Items).Where(item => item.DateSession.Date == day).Sum(item => (long)item.ElapsedSeconds) });
                double max = summed.Max(dpt => dpt.Playtime);
                var withSize = summed.Select(a => { a.Filled = (float)a.Playtime / max; return a; });
                return withSize.ToList();
            }
        }

        private string activityPath;

        public GameActivityViewModel(string activityPath, IPlayniteAPI playniteAPI, LandingPageSettingsViewModel landingPageSettings)
        {
            this.activityPath = activityPath;
            this.playniteAPI = playniteAPI;
            if (activityPath is string && Directory.Exists(activityPath))
            {
                activityWatcher = new FileSystemWatcher(activityPath, "*.json");
                activityWatcher.NotifyFilter = NotifyFilters.LastWrite;
                activityWatcher.Created += ActivityWatcher_Created;
                activityWatcher.Deleted += ActivityWatcher_Deleted;
                activityWatcher.Changed += ActivityWatcher_Changed;
                activityWatcher.EnableRaisingEvents = true;
            }
            this.landingPageSettingsViewModel = landingPageSettings;
            landingPageSettings.PropertyChanged += LandingPageSettings_PropertyChanged;
            landingPageSettings.Settings.PropertyChanged += Settings_PropertyChanged;

        }

        public void ParseAllActivites()
        {
            Activities.Clear();
            if (!string.IsNullOrEmpty(activityPath) && Directory.Exists(activityPath))
            {
                var files = Directory.GetFiles(activityPath);
                var validFiles = files
                    .AsParallel()
                    .Where(path => Guid.TryParse(Path.GetFileNameWithoutExtension(path), out var id) && playniteAPI.Database.Games.Get(id) is Game);
                var deserializedFiles = validFiles
                    .Select(path => DeserializeActivityFile(path))
                    .OfType<Activity>();
                var withSessions = deserializedFiles
                    .Where(ac => (ac.Items?.Count() ?? 0) > 0);
                Activities = withSessions.ToObservable();
            }
        }

        public bool ParseActivities(Guid gameId)
        {
            if (playniteAPI.Database.Games.Get(gameId) is Game)
            {
                if (activityPath != null)
                {
                    var path = Directory.GetFiles(activityPath, gameId.ToString().ToLower() + ".json").FirstOrDefault();
                    if (!string.IsNullOrEmpty(path))
                    {
                        try
                        {
                            var gameAcitivies = DeserializeActivityFile(path);
                            if (gameAcitivies is Activity && gameAcitivies.Items.Any())
                            {
                                if (Activities.FirstOrDefault(a => a.Id == gameAcitivies.Id) is Activity activity)
                                {
                                    Activities.Remove(activity);
                                }
                                Activities.Add(gameAcitivies);
                                return true;
                            }
                        }
                        catch (Exception) { }

                    }
                }
            }
            return false;
        }

        public Activity DeserializeActivityFile(string path)
        {
            try
            {
                using (var fileStream = File.OpenRead(path))
                using (var textReader = new StreamReader(fileStream))
                using (var reader = new JsonTextReader(textReader))
                {
                    var serializer = new JsonSerializer();
                    return serializer.Deserialize<Activity>(reader);
                }
            }
            catch (Exception) { }
            return null;
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            
        }

        private void LandingPageSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Settings" && sender is LandingPageSettingsViewModel settingsViewModel)
            {
                // UpdateLatestAchievements(settingsViewModel.Settings.MaxNumberRecentAchievements, settingsViewModel.Settings.MaxNumberRecentAchievementsPerGame);
                settingsViewModel.Settings.PropertyChanged += Settings_PropertyChanged;
            }
        }

        private void ActivityWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var idString = Path.GetFileNameWithoutExtension(e.Name);
            if (Guid.TryParse(idString, out var id) && playniteAPI.Database.Games.Get(id) is Game)
            {
                if (DeserializeActivityFile(e.FullPath) is Activity activity)
                {
                    if (Activities.FirstOrDefault(a => a.Id == activity.Id) is Activity old)
                    {
                        Activities.Remove(old);
                    }
                    Activities.Add(activity);
                    OnPropertyChanged(nameof(PlaytimeLastWeek));
                    OnPropertyChanged(nameof(PlaytimeLastWeekMax));
                    OnPropertyChanged(nameof(TotalPlaytimeThisWeek));
                }
            }
        }

        private void ActivityWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            var idString = Path.GetFileNameWithoutExtension(e.Name);
            if (Guid.TryParse(idString, out var id))
            {
                if (Activities.FirstOrDefault(a => id == a.Id) is Activity old)
                {
                    Activities.Remove(old);
                    OnPropertyChanged(nameof(PlaytimeLastWeek));
                    OnPropertyChanged(nameof(PlaytimeLastWeekMax));
                    OnPropertyChanged(nameof(TotalPlaytimeThisWeek));
                }
            }
        }

        private void ActivityWatcher_Created(object sender, FileSystemEventArgs e)
        {
            var idString = Path.GetFileNameWithoutExtension(e.Name);
            if (Guid.TryParse(idString, out var id) && playniteAPI.Database.Games.Get(id) is Game)
            {
                if (DeserializeActivityFile(e.FullPath) is Activity activity)
                {
                    if (Activities.FirstOrDefault(a => a.Id == activity.Id) is Activity old)
                    {
                        Activities.Remove(old);
                    }
                    Activities.Add(activity);
                    OnPropertyChanged(nameof(PlaytimeLastWeek));
                    OnPropertyChanged(nameof(PlaytimeLastWeekMax));
                    OnPropertyChanged(nameof(TotalPlaytimeThisWeek));
                }
            }
        }
    }
}
