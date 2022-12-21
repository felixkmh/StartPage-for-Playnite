using PlayniteCommon.Extensions;
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
using StartPage.SDK.Async;
using System.Runtime.CompilerServices;
using LandingPage.Models.Objects;
using System.Windows.Controls;

namespace LandingPage.ViewModels
{
    public class StartPageViewModel : ObservableObject
    {
        private static readonly Random rng = new Random();

        internal IPlayniteAPI playniteAPI;
        internal LandingPageExtension plugin;
        internal DispatcherTimer clock;

        private DateTime time = DateTime.Now;
        public DateTime Time { get => time; private set => SetValue(ref time, value); }

        private GridNodeViewModel rootNodeViewModel;
        public GridNodeViewModel RootNodeViewModel { get => rootNodeViewModel; set => SetValue(ref rootNodeViewModel, value); }

        private BackgroundViewModel backgroundViewModel;
        public BackgroundViewModel BackgroundViewModel { get => backgroundViewModel; set => SetValue(ref backgroundViewModel, value); }

        internal LandingPageSettingsViewModel settings;
        public LandingPageSettingsViewModel Settings => settings;

        public ObservableCollection<NotificationMessage> Notifications { get; private set; }

        public ICommand OpenSettingsCommand => new RelayCommand(() => plugin.OpenSettingsView());
        public ICommand NextRandomBackgroundCommand { get; set; }
        public ICommand DeleteNotificationCommand { get; set; }
        public ICommand ClearNotificationsCommand { get; set; }
        public ICommand EnterEditModeCommand { get; set; }
        public ICommand ExitEditModeCommand { get; set; }


        private bool isLoading = false;
        public bool IsLoading
        {
            get => isLoading;
            set => SetValue(ref isLoading, value);
        }

        public LoadingStatus Status { get; } = new LoadingStatus();

        private bool isInitializing = false;
        public bool IsInitializing
        {
            get => isInitializing;
            set => SetValue(ref isInitializing, value);
        }

        public StartPageViewModel(
            IPlayniteAPI playniteAPI, 
            LandingPageExtension landingPage,
            LandingPageSettingsViewModel settings,
            GridNodeViewModel root,
            BackgroundViewModel backgroundViewModel)
        {
            this.playniteAPI = playniteAPI;
            this.plugin = landingPage;
            this.settings = settings;
            this.rootNodeViewModel = root;
            this.backgroundViewModel = backgroundViewModel;

            clock = new DispatcherTimer(
                TimeSpan.FromSeconds(60 - (DateTime.Now.TimeOfDay.TotalSeconds % 60) + 0.01),
                DispatcherPriority.Normal,
                (sender, args) =>
                {
                    clock.Interval = TimeSpan.FromSeconds(60 - (DateTime.Now.TimeOfDay.TotalSeconds % 60));
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
            NextRandomBackgroundCommand = new RelayCommand(async () =>
            {
                await BackgroundViewModel.UpdateBackgroundImagePathAsync(true);
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
            //BackgroundViewModel.Subscribe();
        }

        public void Unsubscribe()
        {
            //BackgroundViewModel.Unsubscribe();
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

        public async Task Inititalize()
        {
            IsLoading = true;
            IsInitializing = true;
            await BackgroundViewModel.UpdateBackgroundImagePathAsync(Settings.Settings.BackgroundRefreshInterval != 0);
            await RootNodeViewModel.InititalizeGrid();
            await Task.Delay(500);
            await InitializeAndOpenViews();
            IsInitializing = false;
            IsLoading = false;
        }

        private Task InitializeAndOpenViews()
        {
            var tasks = new List<Task>();
            foreach (var node in RootNodeViewModel.AllNodes)
            {
                tasks.Add(node.InitializeAndOpenViewAsync());
            }

            return Task.WhenAll(tasks);
        }

        public async Task Opened()
        {
            var tasks = new List<Task>();
            foreach (var node in RootNodeViewModel.AllNodes.Where(n => n.HasView))
            {
                tasks.Add(node.OpenViewAsync());
            }

            foreach (var node in RootNodeViewModel.AllNodes.Where(n => n.HasView))
            {
                node.OpenView();
            }
            await Task.WhenAll(tasks);

            Subscribe();
        }

        public async Task Closed()
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
                if (view.view is IAsyncStartPageControl asyncControl)
                {
                    try
                    {
                        await asyncControl.OnViewHiddenAsync();
                    }
                    catch (Exception ex)
                    {
                        LandingPageExtension.logger.Warn(ex, "Error when calling OnStartPageOpened()");
                    }
                }
                if (view.view is FrameworkElement asyncElement && asyncElement.DataContext is IAsyncStartPageControl asyncContext)
                {
                    try
                    {
                        await asyncContext.OnViewHiddenAsync();
                    }
                    catch (Exception ex)
                    {
                        LandingPageExtension.logger.Warn(ex, "Error when calling OnStartPageOpened()");
                    }
                }
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
