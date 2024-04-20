using LandingPage.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteCommon.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LandingPage.ViewModels
{
    public class BackgroundViewModel : ObservableObject
    {
        internal Uri backgroundImagePath = null;
        internal ObservableCollection<BackgroundQueueItem> backgroundImageQueue = new ObservableCollection<BackgroundQueueItem>();
        internal DispatcherTimer backgroundImageTimer;
        internal DispatcherTimer backgroundRefreshTimer;
        internal GameModel backgroundSourceGame = null;
        internal Game currentlyHoveredGame;
        internal Game lastHoveredGame;
        internal Game lastSelectedGame;
        private static readonly Random rng = new Random();

        private IPlayniteAPI playniteAPI;

        private LandingPageExtension plugin;

        public BackgroundViewModel(IPlayniteAPI playniteAPI, LandingPageExtension landingPageExtension)
        {
            this.plugin = landingPageExtension;
            this.playniteAPI = playniteAPI;
            this.Settings = landingPageExtension.SettingsViewModel;

            backgroundImageQueue.CollectionChanged += BackgroundImageQueue_CollectionChanged;

            Settings.Settings.PropertyChanged += Settings_PropertyChanged;
            Settings.PropertyChanged += Settings_PropertyChanged1;

            if (playniteAPI.MainView?.SelectedGames?.FirstOrDefault() is Game last)
            {
                lastSelectedGame = last;
            }

            UpdateBackgroundTimer();

            _ = UpdateBackgroundImagePathAsync(Settings.Settings.BackgroundRefreshInterval != 0);

            Subscribe();
        }

        public Uri BackgroundImagePath
        {
            get => backgroundImagePath;
            set
            {
                SetValue(ref backgroundImagePath, value);
                if (BackgroundImageQueue.Count == 0)
                {
                    BackgroundImageQueue.Add(new BackgroundQueueItem(value, 1) { TTL = 0 });
                }
                else if (BackgroundImageQueue.LastOrDefault()?.Uri != value)
                {
                    BackgroundImageQueue.Add(new BackgroundQueueItem(value, 0));
                }
                while(BackgroundImageQueue.Count > 5)
                {
                    BackgroundImageQueue.RemoveAt(0);
                }
            }
        }

        public ObservableCollection<BackgroundQueueItem> BackgroundImageQueue => backgroundImageQueue;
        public GameModel BackgroundSourceGame { get => backgroundSourceGame; set => SetValue(ref backgroundSourceGame, value); }
        public Game CurrentlyHoveredGame
        {
            get => currentlyHoveredGame;
            set
            {
                SetValue(ref currentlyHoveredGame, value);
                if (value is Game && LastHoveredGame != value)
                {
                    LastHoveredGame = value;
                }
            }
        }

        public Game LastHoveredGame
        {
            get => lastHoveredGame;
            set
            {
                SetValue(ref lastHoveredGame, value);
                if (!Settings.Settings.ShowCurrentlyPlayedBackground || plugin.RunningGames.Count == 0)
                    UpdateBackgroundImagePath(false);
            }
        }

        public Game LastSelectedGame
        {
            get => lastSelectedGame;
            set
            {
                SetValue(ref lastSelectedGame, value);
                if (!Settings.Settings.ShowCurrentlyPlayedBackground || plugin.RunningGames.Count == 0)
                    UpdateBackgroundImagePath(false);
            }
        }

        public LandingPageSettingsViewModel Settings { get; set; }
        public void Subscribe()
        {
            playniteAPI.Database.Games.ItemUpdated += Games_ItemUpdated;
            playniteAPI.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
        }

        public void Unsubscribe()
        {
            playniteAPI.Database.Games.ItemUpdated -= Games_ItemUpdated;
            playniteAPI.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
        }

        public void UpdateBackgroundImagePath(bool updateRandomBackground = true)
        {
            Game gameSource = null;
            Uri path = null;
            if (Settings.Settings.BackgroundImageUri is Uri)
            {
                path = Settings.Settings.BackgroundImageUri;
            }
            if (path == null)
            {
                if (System.IO.Directory.Exists(Settings.Settings.BackgroundImagePath))
                {
                    var imagePaths = System.IO.Directory.EnumerateFiles(Settings.Settings.BackgroundImagePath)
                        .Where(file => file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                        || file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                        || file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        .Where(file => BackgroundImagePath?.LocalPath != file);
                    if (BackgroundImagePath?.LocalPath != null

                        && !updateRandomBackground)
                    {
                        path = BackgroundImagePath;
                    }
                    else if (imagePaths.ElementAtOrDefault(rng.Next(imagePaths.Count())) is string imagePath)
                    {
                        if (Uri.TryCreate(imagePath, UriKind.RelativeOrAbsolute, out var uri))
                        {
                            path = uri;
                        }
                    }
                }
            }
            if (path == null)
            {
                switch (Settings.Settings.BackgroundImageSource)
                {
                    case BackgroundImageSource.LastPlayed:
                        {
                            var mostRecent = playniteAPI.Database.Games
                                .OrderByDescending(g => g.LastActivity)
                                .FirstOrDefault(g => !string.IsNullOrEmpty(g.BackgroundImage));
                            var databasePath = mostRecent?.BackgroundImage;
                            if (!string.IsNullOrEmpty(databasePath))
                            {
                                var fullPath = GetFullFilePath(databasePath);
                                if (Uri.TryCreate(fullPath, UriKind.RelativeOrAbsolute, out var uri))
                                {
                                    path = uri;
                                    gameSource = mostRecent;
                                }
                            }
                            break;
                        }
                    case BackgroundImageSource.LastAdded:
                        {
                            var mostRecent = playniteAPI.Database.Games
                                .OrderByDescending(g => g.Added)
                                .FirstOrDefault(g => !string.IsNullOrEmpty(g.BackgroundImage));
                            var databasePath = mostRecent?.BackgroundImage;
                            if (!string.IsNullOrEmpty(databasePath))
                            {
                                var fullPath = GetFullFilePath(databasePath);
                                if (Uri.TryCreate(fullPath, UriKind.RelativeOrAbsolute, out var uri))
                                {
                                    path = uri;
                                    gameSource = mostRecent;
                                }
                            }
                            break;
                        }
                    case BackgroundImageSource.MostPlayed:
                        {
                            var mostPlayed = playniteAPI.Database.Games
                                .Where(g => !string.IsNullOrEmpty(g.BackgroundImage))
                                .MaxElement(game => game.Playtime);
                            if (mostPlayed.Value is Game)
                            {
                                var databasePath = mostPlayed.Value?.BackgroundImage;
                                if (!string.IsNullOrEmpty(databasePath))
                                {
                                    var fullPath = GetFullFilePath(databasePath);
                                    if (Uri.TryCreate(fullPath, UriKind.RelativeOrAbsolute, out var uri))
                                    {
                                        path = uri;
                                        gameSource = mostPlayed.Value;
                                    }
                                }
                            }
                            break;
                        }
                    case BackgroundImageSource.Random:
                        {
                            if (Settings.Settings.LastRandomBackgroundId is Guid id && !updateRandomBackground
                                && playniteAPI.Database.Games.Get(id) is Game lastGame)
                            {
                                var databasePath = lastGame.BackgroundImage;
                                if (!string.IsNullOrEmpty(databasePath))
                                {
                                    var fullPath = GetFullFilePath(databasePath);
                                    if (Uri.TryCreate(fullPath, UriKind.RelativeOrAbsolute, out var uri))
                                    {
                                        path = uri;
                                        gameSource = lastGame;
                                        Settings.Settings.LastRandomBackgroundId = lastGame.Id;
                                    }
                                }
                            }
                            else if (updateRandomBackground || BackgroundImagePath == null)
                            {
                                var candidates = playniteAPI.Database.Games
                                    .Where(game => !game.Hidden)
                                    .Where(game => !string.IsNullOrEmpty(game.BackgroundImage));
                                var randomGame = candidates.ElementAtOrDefault(rng.Next(candidates.Count()));
                                if (randomGame is Game)
                                {
                                    var databasePath = randomGame.BackgroundImage;
                                    if (!string.IsNullOrEmpty(databasePath))
                                    {
                                        var fullPath = GetFullFilePath(databasePath);
                                        if (Uri.TryCreate(fullPath, UriKind.RelativeOrAbsolute, out var uri))
                                        {
                                            path = uri;
                                            gameSource = randomGame;
                                            Settings.Settings.LastRandomBackgroundId = randomGame.Id;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                path = BackgroundImagePath;
                            }
                            break;
                        }
                    case BackgroundImageSource.LastSelected:
                        {
                            if (LastSelectedGame != null && LastSelectedGame.BackgroundImage != null)
                            {
                                var databasePath = LastSelectedGame.BackgroundImage;
                                if (!string.IsNullOrEmpty(databasePath))
                                {
                                    var fullPath = GetFullFilePath(databasePath);
                                    if (Uri.TryCreate(fullPath, UriKind.RelativeOrAbsolute, out var uri))
                                    {
                                        path = uri;
                                        gameSource = lastSelectedGame;
                                    }
                                }
                            }
                        }
                        break;

                    case BackgroundImageSource.LastHovered:
                        {
                            if (LastHoveredGame != null && LastHoveredGame.BackgroundImage != null)
                            {
                                var databasePath = LastHoveredGame.BackgroundImage;
                                if (!string.IsNullOrEmpty(databasePath))
                                {
                                    var fullPath = GetFullFilePath(databasePath);
                                    if (Uri.TryCreate(fullPath, UriKind.RelativeOrAbsolute, out var uri))
                                    {
                                        path = uri;
                                        gameSource = LastHoveredGame;
                                    }
                                }
                            }
                        }
                        break;
                }
            }
            if (path == null)
            {
                var recentlyPlayedGames = playniteAPI.Database.Games.OrderByDescending(g => g.LastActivity);
                var recentlyAddedGames = playniteAPI.Database.Games.OrderByDescending(g => g.Added);
                gameSource = recentlyPlayedGames.OrderByDescending(g => g.LastActivity)
                    .Concat(recentlyAddedGames.OrderByDescending(g => g.Added))
                    .FirstOrDefault(g => !string.IsNullOrEmpty(g.BackgroundImage));
                var databasePath = gameSource?.BackgroundImage;
                if (!string.IsNullOrEmpty(databasePath))
                {
                    var fullPath = GetFullFilePath(databasePath);
                    if (Uri.TryCreate(fullPath, UriKind.RelativeOrAbsolute, out var uri))
                    {
                        path = uri;
                    }
                }
            }
            if (path is Uri && path != BackgroundImagePath)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (backgroundRefreshTimer != null && backgroundRefreshTimer.IsEnabled)
                    {
                        backgroundRefreshTimer.Stop();
                        backgroundRefreshTimer.Start();
                    }
                    BackgroundSourceGame = gameSource != null ? new GameModel(gameSource) : null;
                    BackgroundImagePath = path;
                });
            }
        }

        public void UpdateBackgroundTimer()
        {
            if (Settings.Settings.BackgroundRefreshInterval != 0)
            {
                if (backgroundRefreshTimer == null)
                {
                    backgroundRefreshTimer = new DispatcherTimer(DispatcherPriority.Background, Application.Current.Dispatcher);
                    backgroundRefreshTimer.Tick += async (s, e) =>
                    {
                        if (LandingPageExtension.Instance.RunningGames.Count == 0)
                        {
                            await UpdateBackgroundImagePathAsync(true);
                        }
                    };
                }
                if (backgroundRefreshTimer.Interval.TotalMinutes != Settings.Settings.BackgroundRefreshInterval)
                {
                    backgroundRefreshTimer.Stop();
                    backgroundRefreshTimer.Interval = TimeSpan.FromMinutes(Settings.Settings.BackgroundRefreshInterval);
                }
                if (!backgroundRefreshTimer.IsEnabled)
                {
                    backgroundRefreshTimer.Start();
                }
            }
            else
            {
                backgroundRefreshTimer?.Stop();
                backgroundRefreshTimer = null;
            }
        }

        internal async Task UpdateBackgroundImagePathAsync(bool updateRandomBackground = true)
        {
            await Task.Run(() => UpdateBackgroundImagePath(updateRandomBackground));
        }

        private string GetFullFilePath(string path)
        {
            if (path.StartsWith("http"))
            {
                return path;
            }

            return playniteAPI.Database.GetFullFilePath(path);
        }

        private static bool IsRelevantUpdate(ItemUpdateEvent<Game> update)
        {
            var old = update.OldData;
            var updated = update.NewData;
            if (old.Name != updated.Name) return true;
            if (old.LastActivity != updated.LastActivity) return true;
            if (old.Added != updated.Added) return true;
            if (old.Playtime != updated.Playtime) return true;
            if (old.Hidden != updated.Hidden) return true;
            if (old.Favorite != updated.Favorite) return true;
            if (old.BackgroundImage != updated.BackgroundImage) return true;
            if (old.CoverImage != updated.CoverImage) return true;
            if (old.Icon != updated.Icon) return true;
            var oldPlatformIds = old.PlatformIds ?? new List<Guid>();
            var updatedPlatformIds = updated.PlatformIds ?? new List<Guid>();
            if (!(oldPlatformIds.All(id => updatedPlatformIds.Contains(id)) && oldPlatformIds.Count == updatedPlatformIds.Count)) return true;
            var oldFeatureIds = old.FeatureIds ?? new List<Guid>();
            var updatedFeatureIds = updated.FeatureIds ?? new List<Guid>();
            if (!(oldFeatureIds.All(id => updatedFeatureIds.Contains(id)) && oldFeatureIds.Count == updatedFeatureIds.Count)) return true;
            return false;
        }

        private void BackgroundImageQueue_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                if (BackgroundImageQueue.Count > 1)
                {
                    BackgroundImageQueue[BackgroundImageQueue.Count - 2].TTL = Settings.Settings.AnimationDuration - BackgroundImageQueue[BackgroundImageQueue.Count - 2].TTL;
                }
                if (backgroundImageTimer == null)
                {
                    backgroundImageTimer = new DispatcherTimer(DispatcherPriority.Render, Application.Current.Dispatcher);
                    backgroundImageTimer.Interval = TimeSpan.FromMilliseconds(16);
                    Stopwatch stopwatch = new Stopwatch();
                    backgroundImageTimer.Tick += (_, args) =>
                    {
                        double elapsedSeconds = backgroundImageTimer.Interval.TotalSeconds;
                        if (!stopwatch.IsRunning)
                        {
                            stopwatch.Start();
                        }
                        else
                        {
                            elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                            stopwatch.Restart();
                        }
                        // Debug.WriteLine(string.Join("|", BackgroundImageQueue.Select(i => string.Format("TTL = {0}, Opacity = {1}", i.TTL, i.Opacity))));
                        if (BackgroundImageQueue.Count > 0)
                        {
                            BackgroundImageQueue.ForEach(item => item.TTL -= elapsedSeconds);
                        }
                        for (int i = 0; i < BackgroundImageQueue.Count; ++i)
                        {
                            if (i < BackgroundImageQueue.Count - 1)
                            {
                                BackgroundImageQueue[i].Acceleration -= BackgroundImageQueue[i].Force * elapsedSeconds;
                            }
                            else
                            {
                                BackgroundImageQueue[i].Acceleration += BackgroundImageQueue[i].Force * elapsedSeconds;
                            }
                            BackgroundImageQueue[i].Velocity += BackgroundImageQueue[i].Acceleration * elapsedSeconds;
                            BackgroundImageQueue[i].Position += BackgroundImageQueue[i].Velocity * elapsedSeconds;
                            BackgroundImageQueue[i].Opacity = Math.Sqrt(BackgroundImageQueue[i].Position);
                            BackgroundImageQueue[i].elapsed += elapsedSeconds;
                        }
                        for (int i = BackgroundImageQueue.Count - 2; i >= 0; --i)
                        {
                            if (BackgroundImageQueue[i].Position <= 0)
                            {
                                BackgroundImageQueue.RemoveAt(i);
                            }
                        }
                        if (BackgroundImageQueue.Count > 0 && BackgroundImageQueue.Last().Position >= 1)
                        {
                            // Debug.WriteLine(string.Format("Animation duration: {0}", BackgroundImageQueue.Last().elapsed));
                            BackgroundImageQueue.Last().Velocity = 0;
                            BackgroundImageQueue.Last().Acceleration = 0;
                            BackgroundImageQueue.Last().Opacity = 1;
                            for (int i = BackgroundImageQueue.Count - 2; i >= 0; --i)
                            {
                                BackgroundImageQueue.RemoveAt(i);
                            }
                            backgroundImageTimer.Stop();
                            stopwatch.Stop();
                            GC.Collect();
                        }
                    };
                }
                if (!backgroundImageTimer.IsEnabled && BackgroundImageQueue.Count > 1)
                {
                    backgroundImageTimer.Start();
                }
            }
        }

        private void Games_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> e)
        {
            if (e.RemovedItems.Count + e.AddedItems.Count > 0)
            {
                UpdateBackgroundImagePath(false);
            }
        }

        private void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
        {
            if (plugin.RunningGames.Count > 0)
            {
                return;
            }
            if (e.UpdatedItems.Count == 1)
            {
                if (e.UpdatedItems[0].OldData.IsRunning != e.UpdatedItems[0].NewData.IsRunning)
                {
                    return;
                }
                if (e.UpdatedItems[0].OldData.IsLaunching != e.UpdatedItems[0].NewData.IsLaunching)
                {
                    return;
                }
            }
            if (e.UpdatedItems.Any(u => IsRelevantUpdate(u)))
            {
                UpdateBackgroundImagePath(false);
            }
        }
        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LandingPageSettings.BackgroundImageUri))
            {
                UpdateBackgroundImagePath();
            }
            if (e.PropertyName == nameof(LandingPageSettings.BackgroundImageSource))
            {
                UpdateBackgroundImagePath();
            }
            if (e.PropertyName == nameof(LandingPageSettings.BackgroundRefreshInterval))
            {
                UpdateBackgroundTimer();
            }
            //if (e.PropertyName == nameof(LandingPageSettings.SkipGamesInPreviousShelves))
            //{
            //    Update();
            //}
        }

        private void Settings_PropertyChanged1(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LandingPageSettingsViewModel.Settings))
            {
                UpdateBackgroundImagePath(Settings.Settings.BackgroundRefreshInterval != 0);
                UpdateBackgroundTimer();
                // Update();
                Settings.Settings.PropertyChanged += Settings_PropertyChanged;
            }
        }
    }
}