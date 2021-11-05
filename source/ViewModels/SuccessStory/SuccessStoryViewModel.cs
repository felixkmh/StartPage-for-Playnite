using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using LandingPage.Models.SuccessStory;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.ObjectModel;
using LandingPage.Models;
using LandingPage.Extensions;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LandingPage.ViewModels.SuccessStory
{
    public class SuccessStoryViewModel
    {
        internal string achievementsPath;
        internal IPlayniteAPI playniteAPI;
        internal Dictionary<Guid, Achievements> achievements = new Dictionary<Guid, Achievements>();
        public Dictionary<Guid, Achievements> Achievements => achievements;

        internal LandingPageSettingsViewModel landingPageSettingsViewModel;

        internal FileSystemWatcher achievementWatcher;

        internal ObservableCollection<GameAchievement> latestAchievements = new ObservableCollection<GameAchievement>();
        public ObservableCollection<GameAchievement> LatestAchievements
        {
            get
            {
                return latestAchievements;
            }
        }

        public class GameAchievement : INotifyPropertyChanged
        {
            internal GameModel game;
            public GameModel Game { get => game; set { if (value != game) { game = value; OnPropertyChanged(); } } }
            internal Achivement achievement;
            public Achivement Achievement { get => achievement; set { if (value != achievement) { achievement = value; OnPropertyChanged(); } } }
            internal Achievements source;
            public Achievements Source { get => source; set { if (value != source) { source = value; OnPropertyChanged(); } } }

            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged([CallerMemberName] string propertyName = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public SuccessStoryViewModel(string achievementsPath, IPlayniteAPI playniteAPI, LandingPageSettingsViewModel landingPageSettings)
        {
            this.achievementsPath = achievementsPath;
            this.playniteAPI = playniteAPI;
            achievementWatcher = new FileSystemWatcher(achievementsPath, "*.json");
            achievementWatcher.NotifyFilter = NotifyFilters.LastWrite;
            achievementWatcher.Created += AchievementWatcher_Created;
            achievementWatcher.Deleted += AchievementWatcher_Deleted;
            achievementWatcher.Changed += AchievementWatcher_Changed;
            achievementWatcher.EnableRaisingEvents = true;
            this.landingPageSettingsViewModel = landingPageSettings;
            landingPageSettings.PropertyChanged += LandingPageSettings_PropertyChanged;
            landingPageSettings.Settings.PropertyChanged += Settings_PropertyChanged;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName == nameof(LandingPageSettings.MaxNumberRecentAchievements)
                || e.PropertyName == nameof(LandingPageSettings.MaxNumberRecentAchievementsPerGame))
                && sender is LandingPageSettings settings)
            {
                UpdateLatestAchievements(settings.MaxNumberRecentAchievements, settings.MaxNumberRecentAchievementsPerGame);
            }
        }

        private void LandingPageSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Settings" && sender is LandingPageSettingsViewModel settingsViewModel)
            {
                UpdateLatestAchievements(settingsViewModel.Settings.MaxNumberRecentAchievements, settingsViewModel.Settings.MaxNumberRecentAchievementsPerGame);
                settingsViewModel.Settings.PropertyChanged += Settings_PropertyChanged;
            }
        }

        private void AchievementWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var idString = Path.GetFileNameWithoutExtension(e.Name);
            if (Guid.TryParse(idString, out var id))
            {
                if (ParseAchievements(id))
                {
                    UpdateLatestAchievements(landingPageSettingsViewModel.Settings.MaxNumberRecentAchievements, landingPageSettingsViewModel.Settings.MaxNumberRecentAchievementsPerGame);
                }
            }
        }

        private void AchievementWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            var idString = Path.GetFileNameWithoutExtension(e.Name);
            if (Guid.TryParse(idString, out var id))
            {
                if (achievements.Remove(id))
                {
                    UpdateLatestAchievements(landingPageSettingsViewModel.Settings.MaxNumberRecentAchievements, landingPageSettingsViewModel.Settings.MaxNumberRecentAchievementsPerGame);
                }
            }
        }

        private void AchievementWatcher_Created(object sender, FileSystemEventArgs e)
        {
            var idString = Path.GetFileNameWithoutExtension(e.Name);
            if (Guid.TryParse(idString, out var id))
            {
                if (ParseAchievements(id))
                {
                    UpdateLatestAchievements(landingPageSettingsViewModel.Settings.MaxNumberRecentAchievements, landingPageSettingsViewModel.Settings.MaxNumberRecentAchievementsPerGame);
                }
            }
        }

        public void Update()
        {
            UpdateLatestAchievements(landingPageSettingsViewModel.Settings.MaxNumberRecentAchievements, landingPageSettingsViewModel.Settings.MaxNumberRecentAchievementsPerGame);
        }

        public void UpdateLatestAchievements(int achievementsOverall = 6, int achievementsPerGame = 3)
        {
            var latest = achievements
                .SelectMany(pair => pair.Value.Items
                    .OrderByDescending(a => a.DateUnlocked ?? default)
                    .Take(achievementsPerGame)
                    .Select(a => new { Game = playniteAPI.Database.Games.Get(pair.Value.Id), Achievement = a, Source = pair.Value }))
                .Where(a => (!a.Achievement.DateUnlocked?.Equals(default(DateTime))) ?? false)
                .OrderByDescending(a => a.Achievement.DateUnlocked ?? default)
                .Take(achievementsOverall);
            Application.Current.Dispatcher.Invoke(() => 
            {
                int i = 0;
                foreach (var achievement in latest)
                {
                    if (latestAchievements.Count > i)
                    {
                        latestAchievements[i].Game.Game = achievement.Game;
                        latestAchievements[i].Achievement = achievement.Achievement;
                        latestAchievements[i].Source = achievement.Source;
                    }
                    else
                    {
                        latestAchievements.Add(new GameAchievement
                        {
                            Game = new GameModel(achievement.Game),
                            Achievement = achievement.Achievement,
                            Source = achievement.Source
                        });
                    }
                    ++i;
                }
                for (int j = latestAchievements.Count - 1; j >= i; --j)
                {
                    latestAchievements.RemoveAt(j);
                }
            });
        }

        public void ParseAllAchievements()
        {
            if (Directory.Exists(achievementsPath))
            {
                var files = Directory.GetFiles(achievementsPath);
                var validFiles = files
                    .AsParallel()
                    .Where(path => Guid.TryParse(Path.GetFileNameWithoutExtension(path), out var id) && playniteAPI.Database.Games.Get(id) is Game);
                var deserializedFiles = validFiles
                    .Select(path => DeserializeAchievementsFile(path))
                    .OfType<Achievements>();
                var withAchievements = deserializedFiles
                    .Where(ac => (ac.Items?.Count() ?? 0) > 0);
                achievements = withAchievements.ToDictionary(ac => ac.Id);
            }
        }

        public bool ParseAchievements(Guid gameId)
        {
            if (playniteAPI.Database.Games.Get(gameId) is Game)
            {
                var path = Directory.GetFiles(achievementsPath, gameId.ToString().ToLower() + ".json").FirstOrDefault();
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        var gameAchievements = DeserializeAchievementsFile(path);
                        if (gameAchievements is Achievements && gameAchievements.Items.Any(a => (!a.DateUnlocked?.Equals(default(DateTime))) ?? false))
                        {
                            achievements[gameId] = gameAchievements;
                            return true;
                            
                        }
                    }
                    catch (Exception) {}

                }
            }
            return false;
        }

        internal Achievements DeserializeAchievementsFile(string path)
        {
            try
            {
                using (var fileStream = File.OpenRead(path))
                using (var textReader = new StreamReader(fileStream))
                using (var reader = new JsonTextReader(textReader))
                {
                    var serializer = new JsonSerializer();
                    return serializer.Deserialize<Achievements>(reader);
                }
            }
            catch (Exception) {}
            return null;
        }
    }
}
