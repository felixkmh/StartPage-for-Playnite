using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using LandingPage.Models.GameActivity;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace LandingPage.ViewModels.GameActivity
{
    public class GameActivityViewModel : BusyObservableObject
    {
        internal IPlayniteAPI playniteAPI;
        internal FileSystemWatcher activityWatcher;
        internal LandingPageSettingsViewModel landingPageSettingsViewModel;

        private ObservableCollection<Activity> activities = new ObservableCollection<Activity>();
        public ObservableCollection<Activity> Activities { get => activities; set => SetValue(ref activities, value); }

        public readonly ReaderWriterLockSlim ActivityLock = new ReaderWriterLockSlim();

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

        private ulong playTimeLastWeekMax = 0;
        public ulong PlaytimeLastWeekMax { get => playTimeLastWeekMax; set => SetValue(ref playTimeLastWeekMax, value); }

        private ulong totalPlaytimeThisWeek = 0;
        public ulong TotalPlaytimeThisWeek { get => totalPlaytimeThisWeek; set => SetValue(ref totalPlaytimeThisWeek, value); }

        private List<DayPlaytime> playtimeLastWeek = new List<DayPlaytime>();
        public List<DayPlaytime> PlaytimeLastWeek { get => playtimeLastWeek; set => SetValue(ref playtimeLastWeek, value); }

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
            PropertyChanged += GameActivityViewModel_PropertyChangedAsync;
            Activities.CollectionChanged += Activities_CollectionChanged;
            timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1000) };
            timer.Tick += async (s, a) =>
            {
                var t = (DispatcherTimer)s;
                t.Stop();
                await UpdatePlaytimeLastWeekAsync();
            };
        }

        private async void GameActivityViewModel_PropertyChangedAsync(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Activities))
            {
                Activities.CollectionChanged += Activities_CollectionChanged;
                await UpdatePlaytimeLastWeekAsync();
            }
        }

        public void UpdatePlaytimeLastWeek()
        {
            if (Activities.Count == 0)
            {
                PlaytimeLastWeek = null;
            }
            var lastSevenDays = Enumerable.Range(-6, 7).Reverse().Select(i => DateTime.Today.AddDays(i).Date);
            var summed = lastSevenDays.Select(day => new DayPlaytime { Day = day, Playtime = (ulong)Activities.SelectMany(a => a.Items).Where(item => item.DateSession.Date == day).Sum(item => (long)item.ElapsedSeconds) });
            double max = summed.Max(dpt => dpt.Playtime);
            max = (double.IsNaN(max) || max == 0) ? 1 : max;
            var withSize = summed.Select(a => { a.Filled = (float)a.Playtime / max; a.Filled = double.IsNaN(a.Filled) ? 0 : a.Filled; return a; });
            PlaytimeLastWeek = withSize.ToList();

            PlaytimeLastWeekMax = PlaytimeLastWeek?.Max(dpt => dpt.Playtime) ?? 0;
            TotalPlaytimeThisWeek = (ulong)(PlaytimeLastWeek?.Sum(a => (long)a.Playtime) ?? 0);
        }

        public async Task UpdatePlaytimeLastWeekAsync()
        {
            PlaytimeLastWeek = await Task.Run(() =>
            {
                if (Activities.Count == 0)
                {
                    return null;
                }
                var lastSevenDays = new[] { 6, 5, 4, 3, 2, 1, 0 }.Select(i => DateTime.Today.AddDays(-i).Date);
                var summed = lastSevenDays.Select(day => new DayPlaytime { Day = day, Playtime = (ulong)Activities.SelectMany(a => a.Items).Where(item => item.DateSession.Date == day).Sum(item => (long)item.ElapsedSeconds) });
                double max = summed.Max(dpt => dpt.Playtime);
                max = (double.IsNaN(max) || max == 0) ? 1 : max;
                var withSize = summed.Select(a => { a.Filled = (float)a.Playtime / max; a.Filled = double.IsNaN(a.Filled) ? 0 : a.Filled; return a; });
                return withSize.ToList();
            });
            PlaytimeLastWeekMax = await Task.Run(() => PlaytimeLastWeek?.Max(dpt => dpt.Playtime) ?? 0);
            TotalPlaytimeThisWeek = await Task.Run(() => (ulong)(PlaytimeLastWeek?.Sum(a => (long)a.Playtime) ?? 0));
        }

        public async Task ParseAllActivitiesAsync()
        {
            IsBusy = true;
            var activities = await Task.Run(() =>
            {
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
                    return withSessions.ToObservable();
                }
                return null;
            });

            if (activities is ObservableCollection<Activity>)
            {
                Activities = activities;
                Activities.CollectionChanged += Activities_CollectionChanged;
            }
            IsBusy = false;
        }

        public Task ParseAllActivites()
        {
            return Task.Run(() =>
            {
                IsBusy = true;
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
                    return withSessions.ToObservable();
                }
                return null;
            }).ContinueWith(t =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (t.Result is ObservableCollection<Activity> collection)
                    {
                        Activities = collection;
                        Activities.CollectionChanged += Activities_CollectionChanged;
                    }
                    t?.Dispose();
                }));
                IsBusy = true;
            });
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
                            ActivityLock.EnterWriteLock();
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
                        finally
                        {
                            ActivityLock.ExitWriteLock();
                        }

                    }
                }
            }
            return false;
        }

        public async Task<Activity> DeserializeActivityFileAsync(string path)
        {
            return await Task.Run(() => DeserializeActivityFile(path));
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

        private DispatcherTimer timer = null;

        private void Activities_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            timer.Stop();
            timer.Start();
        }

        private async void ActivityWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var idString = Path.GetFileNameWithoutExtension(e.Name);
                if (Guid.TryParse(idString, out var id) && playniteAPI.Database.Games.Get(id) is Game)
                {
                    if (await DeserializeActivityFileAsync(e.FullPath) is Activity activity)
                    {
                        ActivityLock.EnterWriteLock();
                        if (Activities.FirstOrDefault(a => a.Id == activity.Id) is Activity old)
                        {
                            Activities.Remove(old);
                        }
                        Activities.Add(activity);
                        ActivityLock.ExitWriteLock();
                    }
                }
            });
        }

        private void ActivityWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var idString = Path.GetFileNameWithoutExtension(e.Name);
                if (Guid.TryParse(idString, out var id))
                {
                    ActivityLock.EnterWriteLock();
                    if (Activities.FirstOrDefault(a => id == a.Id) is Activity old)
                    {
                        Activities.Remove(old);
                    }
                    ActivityLock.ExitWriteLock();
                }
            });
        }

        private void ActivityWatcher_Created(object sender, FileSystemEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var idString = Path.GetFileNameWithoutExtension(e.Name);
                if (Guid.TryParse(idString, out var id) && playniteAPI.Database.Games.Get(id) is Game)
                {
                    if (DeserializeActivityFile(e.FullPath) is Activity activity)
                    {
                        ActivityLock.EnterWriteLock();
                        if (Activities.FirstOrDefault(a => a.Id == activity.Id) is Activity old)
                        {
                            Activities.Remove(old);
                        }
                        Activities.Add(activity);
                        ActivityLock.ExitWriteLock();
                    }
                }
            });
        }
    }
}
