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
using StartPage.SDK;
using System.Windows.Threading;

namespace LandingPage.ViewModels.SuccessStory
{
    public class SuccessStoryViewModel : BusyObservableObject, IStartPageViewModel, IStartPageControl
    {
        internal string achievementsPath;
        internal IPlayniteAPI playniteAPI;
        internal Dictionary<Guid, Achievements> achievements = new Dictionary<Guid, Achievements>();
        public Dictionary<Guid, Achievements> Achievements => achievements;

        private event EventHandler<IEnumerable<Guid>> AchievementsUpdated; 

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

        public class GameAchievement : ObservableObject
        {
            internal GameModel game;
            public GameModel Game { get => game; set { if (value != game) { game = value; OnPropertyChanged(); } } }
            internal Achievement achievement;
            public Achievement Achievement { get => achievement; set { if (value != achievement) { achievement = value; OnPropertyChanged(); } } }
            internal Achievements source;
            public Achievements Source { get => source; set { if (value != source) { source = value; OnPropertyChanged(); } } }
        }

        public SuccessStoryViewModel(string achievementsPath, IPlayniteAPI playniteAPI, LandingPageSettingsViewModel landingPageSettings)
        {
            this.achievementsPath = achievementsPath;
            this.playniteAPI = playniteAPI;
            if (achievementsPath is string && Directory.Exists(achievementsPath))
            {
                achievementWatcher = new FileSystemWatcher(achievementsPath, "*.json");
                achievementWatcher.NotifyFilter = NotifyFilters.LastWrite;
                achievementWatcher.Created += AchievementWatcher_CreatedAsync;
                achievementWatcher.Deleted += AchievementWatcher_DeletedAsync;
                achievementWatcher.Changed += AchievementWatcher_ChangedAsync;
                achievementWatcher.EnableRaisingEvents = true;
            }
            this.landingPageSettingsViewModel = landingPageSettings;
            landingPageSettings.PropertyChanged += LandingPageSettings_PropertyChangedAsync;
            landingPageSettings.Settings.PropertyChanged += Settings_PropertyChangedAsync;

            if (Achievements.Count > 0)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    IsBusy = true;
                    UpdateLatestAchievements(
                        landingPageSettingsViewModel.Settings.MaxNumberRecentAchievements,
                        landingPageSettingsViewModel.Settings.MaxNumberRecentAchievementsPerGame
                    );
                    IsBusy = false;
                }), DispatcherPriority.Background);
            }

            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Tick += Timer_Tick;

            AchievementsUpdated += SuccessStoryViewModel_AchievementsUpdated;
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            await UpdateAsync();
        }

        private DispatcherTimer timer;

        private void SuccessStoryViewModel_AchievementsUpdated(object sender, IEnumerable<Guid> e)
        {
            timer?.Stop();
            timer?.Start();
        }

        private async void Settings_PropertyChangedAsync(object sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName == nameof(LandingPageSettings.MaxNumberRecentAchievements)
                || e.PropertyName == nameof(LandingPageSettings.MaxNumberRecentAchievementsPerGame))
                && sender is LandingPageSettings settings)
            {
                await UpdateLatestAchievementsAsync(settings.MaxNumberRecentAchievements, settings.MaxNumberRecentAchievementsPerGame);
            }
        }

        private async void LandingPageSettings_PropertyChangedAsync(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Settings" && sender is LandingPageSettingsViewModel settingsViewModel)
            {
                await UpdateLatestAchievementsAsync(settingsViewModel.Settings.MaxNumberRecentAchievements, settingsViewModel.Settings.MaxNumberRecentAchievementsPerGame);
                settingsViewModel.Settings.PropertyChanged += Settings_PropertyChangedAsync;
            }
        }

        private async void AchievementWatcher_ChangedAsync(object sender, FileSystemEventArgs e)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var idString = Path.GetFileNameWithoutExtension(e.Name);
                if (Guid.TryParse(idString, out var id))
                {
                    if (await ParseAchievementsAsync(id) is Achievements achievements)
                    {
                        Achievements[id] = achievements;
                        AchievementsUpdated?.Invoke(this, new[] { id });
                    }
                }
            });
        }

        private async void AchievementWatcher_DeletedAsync(object sender, FileSystemEventArgs e)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var idString = Path.GetFileNameWithoutExtension(e.Name);
                if (Guid.TryParse(idString, out var id))
                {
                    if (achievements.Remove(id))
                    {
                        AchievementsUpdated?.Invoke(this, new[] { id });
                    }
                }
            });
        }

        private async void AchievementWatcher_CreatedAsync(object sender, FileSystemEventArgs e)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var idString = Path.GetFileNameWithoutExtension(e.Name);
                if (Guid.TryParse(idString, out var id))
                {
                    if (await ParseAchievementsAsync(id) is Achievements achievements)
                    {
                        Achievements[id] = achievements;
                        AchievementsUpdated?.Invoke(this, new[] { id });
                    }
                }
            });
        }

        public void Update()
        {
            Task.Run(() => UpdateLatestAchievements(
                landingPageSettingsViewModel.Settings.MaxNumberRecentAchievements, 
                landingPageSettingsViewModel.Settings.MaxNumberRecentAchievementsPerGame
            ));
        }

        public async Task UpdateAsync()
        {
            IsBusy = true;
            await UpdateLatestAchievementsAsync(
                landingPageSettingsViewModel.Settings.MaxNumberRecentAchievements, 
                landingPageSettingsViewModel.Settings.MaxNumberRecentAchievementsPerGame
            );
            IsBusy = false;
        }

        public class TempAchievement
        {
            public Game Game { get; set; }
            public Achievement Achievement { get; set; }
            public Achievements Source { get; set; }
        }

        public List<TempAchievement> GetLatestAchievements(int achievementsOverall = 6, int achievementsPerGame = 3)
        {
            return Achievements
                .AsParallel()
                .SelectMany(pair => pair.Value.Items
                    .OrderByDescending(a => a.DateUnlocked ?? default)
                    .Take(achievementsPerGame)
                    .Select(a => new TempAchievement { Game = playniteAPI.Database.Games.Get(pair.Value.Id), Achievement = a, Source = pair.Value })
                    .Where(a => a.Game is Game))
                .Where(a => (!a.Achievement.DateUnlocked?.Equals(default)) ?? false)
                .OrderByDescending(a => a.Achievement.DateUnlocked ?? default)
                .Take(achievementsOverall).ToList();
        }

        public async Task<List<TempAchievement>> GetLatestAchievementsAsync(int achievementsOverall = 6, int achievementsPerGame = 3)
        {
            return await Task.Run(() => GetLatestAchievements(achievementsOverall, achievementsPerGame));
        }

        public async Task UpdateLatestAchievementsAsync(int achievementsOverall = 6, int achievementsPerGame = 3)
        {
            var latest = await GetLatestAchievementsAsync(achievementsOverall, achievementsPerGame);
            var collection = LatestAchievements;
            foreach (var achi in latest)
            {
                if (collection.FirstOrDefault(item => item.Game.Game?.Id == achi.Game?.Id && item.Achievement.Name == achi.Achievement.Name) is GameAchievement model)
                {
                    if (model.Achievement.DateUnlocked != achi.Achievement.DateUnlocked)
                    {
                        collection.Remove(model);
                        model.Game.Game = achi.Game;
                        model.Achievement = achi.Achievement;
                        model.Source = achi.Source;
                        collection.Add(model);
                    }
                }
                else if (collection.FirstOrDefault(item => !latest.Any(s => s.Achievement.Name == item.Achievement.Name && s.Game?.Id == item.Game.Game?.Id)) is GameAchievement unusedModel)
                {
                    collection.Remove(unusedModel);
                    unusedModel.Game.Game = achi.Game;
                    unusedModel.Achievement = achi.Achievement;
                    unusedModel.Source = achi.Source;
                    collection.Add(unusedModel);
                }
                else
                {
                    collection.Add(new GameAchievement
                    {
                        Game = new GameModel(achi.Game),
                        Achievement = achi.Achievement,
                        Source = achi.Source
                    });
                }
            }
            for (int j = collection.Count - 1; j >= 0; --j)
            {
                if (!latest.Any(g => g.Achievement.Name == collection[j].Achievement.Name && g.Game?.Id == collection[j].Game.Game?.Id))
                {
                    collection.RemoveAt(j);
                }
            }
        }

        public void UpdateLatestAchievements(int achievementsOverall = 6, int achievementsPerGame = 3)
        {
            var latest = GetLatestAchievements(achievementsOverall, achievementsPerGame);
            var collection = LatestAchievements;
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var achi in latest)
                {
                    if (collection.FirstOrDefault(item => item.Game.Game?.Id == achi.Game?.Id && item.Achievement.Name == achi.Achievement.Name) is GameAchievement model)
                    {
                        if (model.Achievement.DateUnlocked != achi.Achievement.DateUnlocked)
                        {
                            collection.Remove(model);
                            model.Game.Game = achi.Game;
                            model.Achievement = achi.Achievement;
                            model.Source = achi.Source;
                            collection.Add(model);
                        }
                    }
                    else if (collection.FirstOrDefault(item => !latest.Any(s => s.Achievement.Name == item.Achievement.Name && s.Game?.Id == item.Game.Game?.Id)) is GameAchievement unusedModel)
                    {
                        collection.Remove(unusedModel);
                        unusedModel.Game.Game = achi.Game;
                        unusedModel.Achievement = achi.Achievement;
                        unusedModel.Source = achi.Source;
                        collection.Add(unusedModel);
                    }
                    else
                    {
                        collection.Add(new GameAchievement
                        {
                            Game = new GameModel(achi.Game),
                            Achievement = achi.Achievement,
                            Source = achi.Source
                        });
                    }
                }
                for (int j = collection.Count - 1; j >= 0; --j)
                {
                    if (!latest.Any(g => g.Achievement.Name == collection[j].Achievement.Name && g.Game?.Id == collection[j].Game.Game?.Id))
                    {
                        collection.RemoveAt(j);
                    }
                }
            });
        }

        public void ParseAllAchievements()
        {
            if (achievementsPath != null)
            {
                if (achievementsPath != null)
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
                        // achievements = withAchievements.ToDictionary(ac => ac.Id);
                        achievements = withAchievements.ToDictionary(ac => ac.Id);
                        AchievementsUpdated?.Invoke(this, achievements.Keys);
                    }
                }
            }
        }

        public async Task ParseAllAchievementsAsync()
        {
            await Task.Run(ParseAllAchievements);
        }

        public async Task<Achievements> ParseAchievementsAsync(Guid gameId)
        {
            return await Task.Run(() => ParseAchievements(gameId));
        }

        public Achievements ParseAchievements(Guid gameId)
        {
            if (playniteAPI.Database.Games.Get(gameId) is Game)
            {
                if (achievementsPath != null)
                {
                    var path = Directory.GetFiles(achievementsPath, gameId.ToString().ToLower() + ".json").FirstOrDefault();
                    if (!string.IsNullOrEmpty(path))
                    {
                        try
                        {
                            var gameAchievements = DeserializeAchievementsFile(path);
                            if (gameAchievements is Achievements && gameAchievements.Items.Any(a => (!a.DateUnlocked?.Equals(default)) ?? false))
                            {
                                return gameAchievements;
                            }
                        }
                        catch (Exception) {}

                    }
                }
            }
            return null;
        }

        internal async Task<Achievements> DeserializeAchievementsFileAsync(string path)
        {
            return await Task.Run(() => DeserializeAchievementsFile(path));
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

        public void OnViewClosed()
        {
            achievementWatcher.EnableRaisingEvents = false;
            landingPageSettingsViewModel.PropertyChanged -= LandingPageSettings_PropertyChangedAsync;
            landingPageSettingsViewModel.Settings.PropertyChanged -= Settings_PropertyChangedAsync;
            achievementWatcher.Dispose();
        }

        public void OnStartPageOpened()
        {
            //await UpdateAsync();
        }

        public void OnStartPageClosed()
        {
            
        }

        public void OnDayChanged(DateTime newTime)
        {
            
        }
    }
}
