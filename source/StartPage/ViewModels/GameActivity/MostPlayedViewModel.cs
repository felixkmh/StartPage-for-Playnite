using LandingPage.Converters;
using LandingPage.Extensions;
using LandingPage.Models;
using LandingPage.Models.GameActivity;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LandingPage.ViewModels.GameActivity
{
    public class MostPlayedViewModel : ObservableObject
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
            settingsViewModel.Settings.PropertyChanged += Settings_PropertyChanged;
            settingsViewModel.PropertyChanged += SettingsViewModel_PropertyChanged;
            foreach(var options in settingsViewModel.Settings.MostPlayedOptions)
            {
                options.PropertyChanged += Options_PropertyChanged;
            }
            gameActivityViewModel.Activities.CollectionChanged += Activities_CollectionChanged;
            UpdateMostPlayedGame();
        }

        private void Options_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateMostPlayedGame();
        }

        private void SettingsViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LandingPageSettingsViewModel.Settings))
            {
                foreach (var options in Settings.Settings.MostPlayedOptions)
                {
                    options.PropertyChanged += Options_PropertyChanged;
                }
                Settings.Settings.PropertyChanged += Settings_PropertyChanged;
                UpdateMostPlayedGame();
            }
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LandingPageSettings.MostPlayedOptions))
            {
                foreach (var options in Settings.Settings.MostPlayedOptions)
                {
                    options.PropertyChanged += Options_PropertyChanged;
                }
                UpdateMostPlayedGame();
            }
        }

        private void Activities_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateMostPlayedGame();
        }

        private void UpdateMostPlayedGame()
        {
            var groups = new List<GameGroup>();

            var tagId = LandingPageExtension.Instance.SettingsViewModel.Settings.IgnoreMostPlayedTagId;

            foreach(var options in Settings.Settings.MostPlayedOptions)
            {
                if (options.Timeframe == Timeframe.None)
                {
                    continue;
                }
                if (options.Timeframe == Timeframe.AllTime)
                {
                    if (playniteAPI.Database.Games
                        .Where(g => !g.Hidden)
                        .Where(g => !(g.TagIds?.Contains(tagId) ?? false))
                        .MaxElement(g => g.Playtime).Value is Game game)
                    {
                        var group = new GameGroup();
                        group.Games.Add(new GameModel(game));
                        group.Label = converter.ConvertToString(options.Timeframe);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            groups.Add(group);
                        });
                    }
                } else
                {
                    var thisWeek = GameActivityViewModel.Activities
                        .Select(a => new { Game = playniteAPI.Database.Games.Get(a.Id), Items = a.Items.Where(i => options.Timeframe == Timeframe.AllTime || i.DateSession.Add(options.TimeSpan) >= DateTime.Today).ToList() })
                        .Where(a => a.Game is Game && a.Items?.Count > 0)
                        .Where(a => !(a.Game.TagIds?.Contains(tagId) ?? false));
                    var mostPlayedThisWeek = thisWeek
                        .Select(a => new { Game = a.Game, Playtime = a.Items.Sum(i => (long)i.ElapsedSeconds) })
                        .MaxElement(g => g.Playtime).Value?.Game;

                    if (mostPlayedThisWeek is Game mostPlayedWeek)
                    {
                        var group = new GameGroup();
                        group.Games.Add(new GameModel(mostPlayedWeek));
                        group.Label = converter.ConvertToString(options.Timeframe);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            groups.Add(group);
                        });
                    }
                }
            }

            for (int i = 0; i < groups.Count; i++)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (specialGames.Count > i)
                    {
                        specialGames[i].Label = groups[i].Label;
                        specialGames[i].Games[0].Game = groups[i].Games[0].Game;
                    } else
                    {
                        specialGames.Add(groups[i]);
                    }
                });
            }

            for(int i = specialGames.Count - 1; i >= groups.Count; --i)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    specialGames.RemoveAt(i);
                });
            }
        }
    }
}
