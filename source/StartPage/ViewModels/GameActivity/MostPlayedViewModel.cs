using LandingPage.Converters;
using PlayniteCommon.Extensions;
using LandingPage.Models;
using LandingPage.Models.GameActivity;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Threading;

namespace LandingPage.ViewModels.GameActivity
{
    public class MostPlayedViewModel : ObservableObject, IStartPageViewModel
    {
        private static readonly EnumDescriptionTypeConverter converter = new EnumDescriptionTypeConverter(typeof(Timeframe));

        public GameActivityViewModel GameActivityViewModel { get; set; }

        internal ObservableCollection<GameGroup> specialGames = new ObservableCollection<GameGroup>();
        public ObservableCollection<GameGroup> SpecialGames => specialGames;

        internal IPlayniteAPI playniteAPI;
        internal LandingPageSettingsViewModel settings;
        public LandingPageSettingsViewModel Settings => settings;

        public MostPlayedViewModel(IPlayniteAPI playniteApi, LandingPageSettingsViewModel settingsViewModel, GameActivityViewModel gameActivityViewModel)
        {
            GameActivityViewModel = gameActivityViewModel;
            this.playniteAPI = playniteApi;
            this.settings = settingsViewModel;
            settingsViewModel.Settings.PropertyChanged += Settings_PropertyChangedAsync;
            settingsViewModel.PropertyChanged += SettingsViewModel_PropertyChangedAsync;
            settingsViewModel.Settings.MostPlayedOptions.CollectionChanged += MostPlayedOptions_CollectionChanged;
            foreach (var options in settingsViewModel.Settings.MostPlayedOptions)
            {
                options.PropertyChanged += Options_PropertyChangedAsync;
            }
            gameActivityViewModel.Activities.CollectionChanged += Activities_CollectionChanged;
            gameActivityViewModel.PropertyChanged += GameActivityViewModel_PropertyChangedAsync;
            FillWithPlaceholders();
            if (GameActivityViewModel.Activities.Count > 0)
            {
                UpdateMostPlayedGame();
            }
        }

        private async void GameActivityViewModel_PropertyChangedAsync(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GameActivityViewModel.Activities))
            {
                GameActivityViewModel.Activities.CollectionChanged += Activities_CollectionChanged;
                await UpdateMostPlayedAsync();
            }
        }

        private async void Options_PropertyChangedAsync(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            await UpdateMostPlayedAsync();
        }

        private async void SettingsViewModel_PropertyChangedAsync(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LandingPageSettingsViewModel.Settings))
            {
                Settings.Settings.MostPlayedOptions.CollectionChanged += MostPlayedOptions_CollectionChanged;
                foreach (var options in Settings.Settings.MostPlayedOptions)
                {
                    options.PropertyChanged -= Options_PropertyChangedAsync;
                    options.PropertyChanged += Options_PropertyChangedAsync;
                }
                Settings.Settings.PropertyChanged += Settings_PropertyChangedAsync;
                await UpdateMostPlayedAsync();
            }
        }

        private async void Settings_PropertyChangedAsync(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LandingPageSettings.MostPlayedOptions))
            {
                Settings.Settings.MostPlayedOptions.CollectionChanged -= MostPlayedOptions_CollectionChanged;
                Settings.Settings.MostPlayedOptions.CollectionChanged += MostPlayedOptions_CollectionChanged;
                foreach (var options in Settings.Settings.MostPlayedOptions)
                {
                    options.PropertyChanged -= Options_PropertyChangedAsync;
                    options.PropertyChanged += Options_PropertyChangedAsync;
                }
                await UpdateMostPlayedAsync();
            }
            if (e.PropertyName == nameof(LandingPageSettings.SkipGamesInPreviousMostPlayed))
            {
                await UpdateMostPlayedAsync();
            }
        }

        private void MostPlayedOptions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (MostPlayedOptions removed in e.OldItems)
                {
                    removed.PropertyChanged -= Options_PropertyChangedAsync;
                }
            if (e.NewItems != null)
                foreach(MostPlayedOptions added in e.NewItems)
                {
                    added.PropertyChanged += Options_PropertyChangedAsync;
                }
        }

        private DispatcherTimer timer = null;

        private void Activities_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (timer == null)
            {
                timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                timer.Tick += async (s, a) =>
                {
                    if (s is DispatcherTimer dt)
                    {
                        dt.Stop();
                    }
                    await UpdateMostPlayedAsync();
                };
            }
            timer.Stop();
            timer.Start();
        }

        Task backgroundTask = Task.CompletedTask;

        public async Task UpdateMostPlayedAsync()
        {
            var tagId = LandingPageExtension.Instance.SettingsViewModel.Settings.IgnoreMostPlayedTagId;

            for (int i = Settings.Settings.MostPlayedOptions.Count - 1; i > 0; --i)
            {
                if (Settings.Settings.MostPlayedOptions[i].Timeframe == Timeframe.None &&
                    Settings.Settings.MostPlayedOptions[i - 1].Timeframe == Timeframe.None)
                {
                    Settings.Settings.MostPlayedOptions.RemoveAt(i);
                    continue;
                }
                break;
            }

            if (Settings.Settings.MostPlayedOptions.LastOrDefault() is MostPlayedOptions option &&
                option.Timeframe != Timeframe.None)
            {
                Settings.Settings.MostPlayedOptions.Add(new MostPlayedOptions { Timeframe = Timeframe.None });
            }

            if (Settings.Settings.MostPlayedOptions.Count == 0)
            {
                Settings.Settings.MostPlayedOptions.Add(new MostPlayedOptions { Timeframe = Timeframe.None });
            }

            var groups = await GetMostPlayedGroupsAsync();

            for (int i = 0; i < groups.Count; i++)
            {
                if (specialGames.Count > i)
                {
                    specialGames[i].Label = groups[i].Label;
                    specialGames[i].Games.Clear();
                    foreach(var game in groups[i].Games)
                    {
                        specialGames[i].Games.Add(game);
                    }
                }
                else
                {
                    specialGames.Add(groups[i]);
                }
            }

            int j = specialGames.Count - 1;
            while (specialGames.Count > groups.Count)
            {
                specialGames.RemoveAt(j--);
            }
        }

        public void UpdateMostPlayedGame()
        {
            backgroundTask = backgroundTask.ContinueWith(t =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    for (int i = Settings.Settings.MostPlayedOptions.Count - 1; i > 0; --i)
                    {
                        if (Settings.Settings.MostPlayedOptions[i].Timeframe == Timeframe.None &&
                            Settings.Settings.MostPlayedOptions[i - 1].Timeframe == Timeframe.None)
                        {
                            Settings.Settings.MostPlayedOptions.RemoveAt(i);
                            continue;
                        }
                        break;
                    }

                    if (Settings.Settings.MostPlayedOptions.LastOrDefault() is MostPlayedOptions option &&
                        option.Timeframe != Timeframe.None)
                    {
                        Settings.Settings.MostPlayedOptions.Add(new MostPlayedOptions { Timeframe = Timeframe.None });
                    }

                    if (Settings.Settings.MostPlayedOptions.Count == 0)
                    {
                        Settings.Settings.MostPlayedOptions.Add(new MostPlayedOptions { Timeframe = Timeframe.None });
                    }
                });
                t?.Dispose();
                return GetMostPlayedGroups();
            }).ContinueWith(t =>
            {
                var groups = t.Result;
                for (int i = 0; i < groups.Count; i++)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (specialGames.Count > i)
                        {
                            specialGames[i].Label = groups[i].Label;
                            specialGames[i].Games[0].Game = groups[i].Games[0].Game;
                        }
                        else
                        {
                            specialGames.Add(groups[i]);
                        }
                    });
                }

                int j = specialGames.Count - 1;
                while(specialGames.Count > groups.Count)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        specialGames.RemoveAt(j--);
                    });
                }

                t?.Dispose();
            });
        }

        private async Task<List<GameGroup>> GetMostPlayedGroupsAsync()
        {
            return await Task.Run(GetMostPlayedGroups);
        }

        private List<GameGroup> GetMostPlayedGroups()
        {
            var groups = new List<GameGroup>();
            var tagId = LandingPageExtension.Instance.SettingsViewModel.Settings.IgnoreMostPlayedTagId;

            foreach (var options in Settings.Settings.MostPlayedOptions)
            {
                if (options.Timeframe == Timeframe.None)
                {
                    continue;
                }
                if (options.Timeframe == Timeframe.AllTime)
                {
                    var candidates = playniteAPI.Database.Games
                        .Where(g => !g.Hidden)
                        .Where(g => !(g.TagIds?.Contains(tagId) ?? false));
                    candidates = candidates
                        .OrderByDescending(a => a.Playtime)
                        .Skip(Math.Max(0, Math.Min(options.SkippedGames, candidates.Count())));
                    candidates = candidates.Where(g => !settings.Settings.SkipGamesInPreviousMostPlayed || !groups.Any(group => group.Games.Any(m => m.Game == g)));
                    if (candidates.FirstOrDefault() is Game game)
                    {
                        var group = new GameGroup();
                        group.Games.Add(new GameModel(game));
                        group.Label = converter.ConvertToString(options.Timeframe);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            groups.Add(group);
                        });
                    }
                }
                else
                {
                    GameActivityViewModel.ActivityLock.EnterReadLock();

                    var thisWeek = GameActivityViewModel.Activities
                        .Select(a => new { Game = playniteAPI.Database.Games.Get(a.Id), Items = a.Items.Where(i => options.Timeframe == Timeframe.AllTime || i.DateSession.Add(options.TimeSpan) >= DateTime.Today).ToList() })
                        .Where(a => a.Game is Game && a.Items?.Count > 0)
                        .Where(a => !(a.Game.TagIds?.Contains(tagId) ?? false))
                        .Select(a => new { Game = a.Game, Playtime = a.Items.Sum(i => (long)i.ElapsedSeconds) });
                    thisWeek = thisWeek
                        .OrderByDescending(a => a.Playtime)
                        .Skip(Math.Max(0, Math.Min(options.SkippedGames, thisWeek.Count())));

                    thisWeek = thisWeek.Where(a => !settings.Settings.SkipGamesInPreviousMostPlayed || !groups.Any(group => group.Games.Any(m => m.Game == a.Game)));
                    var mostPlayedThisWeek = thisWeek.FirstOrDefault();

                    GameActivityViewModel.ActivityLock.ExitReadLock();

                    if (mostPlayedThisWeek?.Game is Game)
                    {
                        var group = groups.LastOrDefault();
                        var groupLabel = converter.ConvertToString(options.Timeframe);
                        if (groupLabel != group?.Label)
                        {
                            group = new GameGroup() { Label = groupLabel };
                        }
                        group.Games.Add(new GameModel(mostPlayedThisWeek.Game));
                        if (!groups.Contains(group))
                        {
                            groups.Add(group);
                        }
                    }
                }
            }
            return groups;
        }

        public void FillWithPlaceholders()
        {
            var specialGames = SpecialGames;
            var groups = GetPlaceholderGroups();
            for (int i = 0; i < groups.Count; i++)
            {
                if (specialGames.Count > i)
                {
                    specialGames[i].Label = groups[i].Label;
                    specialGames[i].Games[0].Game = groups[i].Games[0].Game;
                }
                else
                {
                    specialGames.Add(groups[i]);
                }
            }

            int j = specialGames.Count - 1;
            while (specialGames.Count > groups.Count)
            {
                specialGames.RemoveAt(j--);
            }
        }

        private List<GameGroup> GetPlaceholderGroups()
        {
            var groups = new List<GameGroup>();
            var tagId = LandingPageExtension.Instance.SettingsViewModel.Settings.IgnoreMostPlayedTagId;

            foreach (var options in Settings.Settings.MostPlayedOptions)
            {
                if (options.Timeframe == Timeframe.None)
                {
                    continue;
                }
               var group = new GameGroup();
                group.Games.Add(new GameModel(ShelveViewModel.DummyGame));
                group.Label = converter.ConvertToString(options.Timeframe);
                groups.Add(group);
            }
            return groups;
        }

        public void OnViewClosed()
        {
            GameActivityViewModel.Activities.CollectionChanged -= Activities_CollectionChanged;
            GameActivityViewModel.PropertyChanged -= GameActivityViewModel_PropertyChangedAsync;
        }
    }
}
