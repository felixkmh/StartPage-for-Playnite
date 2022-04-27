using LandingPage.Extensions;
using LandingPage.Models;
using LandingPage.Models.Layout;
using LandingPage.ViewModels.Layout;
using Playnite.SDK;
using Playnite.SDK.Models;
using StartPage.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace LandingPage.ViewModels
{
    public class StartPageViewModel : ObservableObject
    {
        private static readonly Random rng = new Random();

        internal IPlayniteAPI playniteAPI;
        internal LandingPageExtension plugin;
        internal DispatcherTimer backgroundImageTimer;
        internal DispatcherTimer backgroundRefreshTimer;
        internal DispatcherTimer clock;

        private DateTime time = DateTime.Now;
        public DateTime Time { get => time; private set => SetValue(ref time, value); }

        private GridNodeViewModel rootNodeViewModel;
        public GridNodeViewModel RootNodeViewModel { get => rootNodeViewModel; set => SetValue(ref rootNodeViewModel, value); }

        internal ObservableCollection<BackgroundQueueItem> backgroundImageQueue = new ObservableCollection<BackgroundQueueItem>();
        public ObservableCollection<BackgroundQueueItem> BackgroundImageQueue => backgroundImageQueue;

        internal GameModel backgroundSourceGame = null;
        public GameModel BackgroundSourceGame { get => backgroundSourceGame; set => SetValue(ref backgroundSourceGame, value); }

        internal LandingPageSettingsViewModel settings;
        public LandingPageSettingsViewModel Settings => settings;

        public ObservableCollection<NotificationMessage> Notifications { get; private set; }

        public ICommand OpenSettingsCommand => new RelayCommand(() => plugin.OpenSettingsView());
        public ICommand NextRandomBackgroundCommand { get; set; }
        public ICommand DeleteNotificationCommand { get; set; }
        public ICommand ClearNotificationsCommand { get; set; }
        public ICommand EnterEditModeCommand { get; set; }
        public ICommand ExitEditModeCommand { get; set; }

        internal Uri backgroundImagePath = null;
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
            }
        }

        internal Game lastSelectedGame;
        public Game LastSelectedGame { get => lastSelectedGame; set { SetValue(ref lastSelectedGame, value); UpdateBackgroundImagePath(false); } }

        internal Game lastHoveredGame;
        public Game LastHoveredGame { get => lastHoveredGame; set { SetValue(ref lastHoveredGame, value); UpdateBackgroundImagePath(false); } }

        internal Game currentlyHoveredGame;
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

        public StartPageViewModel(
            IPlayniteAPI playniteAPI, 
            LandingPageExtension landingPage,
            LandingPageSettingsViewModel settings,
            GridNodeViewModel root)
        {
            this.playniteAPI = playniteAPI;
            this.plugin = landingPage;
            this.settings = settings;
            this.rootNodeViewModel = root;
            clock = new DispatcherTimer(
                TimeSpan.FromSeconds(60 - (DateTime.Now.TimeOfDay.TotalSeconds % 60) + 0.01),
                DispatcherPriority.Normal,
                (sender, args) =>
                {
                    clock.Interval = TimeSpan.FromSeconds(60);
                    var currentTime = DateTime.Now.RoundToClosestMinute();
                    if (currentTime != Time)
                    {
                        if (currentTime.DayOfYear != Time.DayOfYear)
                        {
                            DayChanged(Time);
                        }
                        Time = currentTime;
                    }
                },
                Application.Current.Dispatcher
            );

            Notifications = playniteAPI.Notifications.Messages;
            Notifications.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(Notifications));
            DeleteNotificationCommand = new RelayCommand<NotificationMessage>(sender => playniteAPI.Notifications.Remove(sender.Id));
            ClearNotificationsCommand = new RelayCommand(() => playniteAPI.Notifications.RemoveAll());
            NextRandomBackgroundCommand = new RelayCommand(() =>
            {
                UpdateBackgroundImagePath(true);
            }, () => (Settings.Settings.BackgroundImageSource == BackgroundImageSource.Random && !System.IO.File.Exists(Settings.Settings.BackgroundImagePath))
            || System.IO.Directory.Exists(Settings.Settings.BackgroundImagePath));

            EnterEditModeCommand = new RelayCommand(() => 
                RootNodeViewModel.EditModeEnabled = true
            );
            ExitEditModeCommand = new RelayCommand(() =>
            {
                RootNodeViewModel.EditModeEnabled = false;
                GridNode.Minimize(RootNodeViewModel.GridNode, null);
                settings.Settings.GridLayout = RootNodeViewModel.GridNode;
                landingPage.SavePluginSettings(settings.Settings);
                GC.Collect();
            });

            Settings.Settings.PropertyChanged += Settings_PropertyChanged;
            Settings.PropertyChanged += Settings_PropertyChanged1;
            backgroundImageQueue.CollectionChanged += BackgroundImageQueue_CollectionChanged;

            UpdateBackgroundImagePath(true);
            UpdateBackgroundTimer();
        }

        private void DayChanged(DateTime newTime)
        {
            foreach(var properties in RootNodeViewModel.ActiveViews)
            {
                if (properties.view is IStartPageControl control)
                {
                    try
                    {
                        control.OnDayChanged(newTime);
                    }
                    catch (Exception ex)
                    {
                        LandingPageExtension.logger.Warn(ex, $"Error when calling OnDayChanged() on control for instance {properties.InstanceId} of viewId {properties.ViewId}.");
                    }
                }
                if (properties.view is FrameworkElement element && element.DataContext is IStartPageControl context)
                {
                    try
                    {
                        context.OnDayChanged(newTime);
                    }
                    catch (Exception ex)
                    {
                        LandingPageExtension.logger.Warn(ex, $"Error when calling OnDayChanged() on dataContext for instance {properties.InstanceId} of viewId {properties.ViewId}.");
                    }
                }
            }
        }

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

        private void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
        {
            if (e.UpdatedItems.Any(u => IsRelevantUpdate(u)))
            {
                UpdateBackgroundImagePath(false);
            }
        }

        private void Games_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> e)
        {
            if (e.RemovedItems.Count + e.AddedItems.Count > 0)
            {
                UpdateBackgroundImagePath(false);
            }
        }

        public void Opened()
        {
            Subscribe();
            foreach(var view in RootNodeViewModel.ActiveViews)
            {
                if (view.view is IStartPageControl control)
                {
                    try
                    {
                        control.OnStartPageOpened();
                    }
                    catch (Exception ex)
                    {
                        LandingPageExtension.logger.Warn(ex, "Error when calling OnStartPageOpened()");
                    }
                }
                if (view.view is FrameworkElement element && element.DataContext is IStartPageControl context)
                {
                    try
                    {
                        context.OnStartPageOpened();
                    }
                    catch (Exception ex)
                    {
                        LandingPageExtension.logger.Warn(ex, "Error when calling OnStartPageOpened()");
                    }
                }
            }
        }

        public void Closed()
        {
            RootNodeViewModel.EditModeEnabled = false;
            Unsubscribe();
            foreach (var view in RootNodeViewModel.ActiveViews)
            {
                if (view.view is IStartPageControl control)
                {
                    try
                    {
                        control.OnStartPageClosed();
                    }
                    catch (Exception ex)
                    {
                        LandingPageExtension.logger.Warn(ex, "Error when calling OnStartPageClosed()");
                    }
                }
                if (view.view is FrameworkElement element && element.DataContext is IStartPageControl context)
                {
                    try
                    {
                        context.OnStartPageClosed();
                    }
                    catch (Exception ex)
                    {
                        LandingPageExtension.logger.Warn(ex, "Error when calling OnStartPageClosed()");
                    }
                }
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
                UpdateBackgroundImagePath();
                UpdateBackgroundTimer();
                // Update();
                Settings.Settings.PropertyChanged += Settings_PropertyChanged;
            }
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

        private void UpdateBackgroundTimer()
        {
            if (Settings.Settings.BackgroundRefreshInterval != 0)
            {
                if (backgroundRefreshTimer == null)
                {
                    backgroundRefreshTimer = new DispatcherTimer(DispatcherPriority.Background, Application.Current.Dispatcher);
                    backgroundRefreshTimer.Tick += (s, e) =>
                    {
                        if (LandingPageExtension.Instance.RunningGames.Count == 0)
                        {
                            UpdateBackgroundImagePath(true);
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

        private void UpdateBackgroundImagePath(bool updateRandomBackground = true)
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
                                var fullPath = playniteAPI.Database.GetFullFilePath(databasePath);
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
                                var fullPath = playniteAPI.Database.GetFullFilePath(databasePath);
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
                                    var fullPath = playniteAPI.Database.GetFullFilePath(databasePath);
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
                            if (updateRandomBackground || BackgroundImagePath == null)
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
                                        var fullPath = playniteAPI.Database.GetFullFilePath(databasePath);
                                        if (Uri.TryCreate(fullPath, UriKind.RelativeOrAbsolute, out var uri))
                                        {
                                            path = uri;
                                            gameSource = randomGame;
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
                                    var fullPath = playniteAPI.Database.GetFullFilePath(databasePath);
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
                                    var fullPath = playniteAPI.Database.GetFullFilePath(databasePath);
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
                    var fullPath = playniteAPI.Database.GetFullFilePath(databasePath);
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

        public class BackgroundQueueItem : ObservableObject
        {
            public BackgroundQueueItem(Uri uri, double opacity)
            {
                Uri = uri;
                Opacity = opacity;
                Position = opacity;
                TTL = LandingPageExtension.Instance.Settings.AnimationDuration;
                var T = LandingPageExtension.Instance.Settings.AnimationDuration;
                var a0 = 0;
                Acceleration = a0;
                if (T > 0)
                {
                    Force = (6 - 3 * a0 * T * T) / (T * T * T);
                }
                else
                {
                    Position = 1;
                }
            }
            public const double MaxTTL = 0;
            internal Uri uri;
            public Uri Uri { get => uri; set => SetValue(ref uri, value); }
            internal double ttl = 0;
            public double TTL { get => ttl; set => SetValue(ref ttl, value); }
            public double Velocity { get; set; } = 0;
            public double Acceleration { get; set; } = 0;
            public double Force { get; set; }
            internal double position = 0;
            public double Position { get => Math.Max(0, Math.Min(1, position)); set => SetValue(ref position, value); }
            internal double opacity = 0;
            public double elapsed = 0;
            public double Opacity { get => Math.Max(0, Math.Min(1, opacity)); set => SetValue(ref opacity, value); }
        }
    }
}
