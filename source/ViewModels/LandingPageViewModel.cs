using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Playnite.SDK;
using Playnite.SDK.Models;
using LandingPage.Models;
using System.Windows.Data;
using System.Windows;
using LandingPage.Extensions;

namespace LandingPage.ViewModels
{
    public class LandingPageViewModel
    {
        internal ObservableCollection<GameGroup> recentlyPlayedGames = new ObservableCollection<GameGroup>();
        public ObservableCollection<GameGroup> RecentlyPlayedGames => recentlyPlayedGames;

        internal ObservableCollection<GameModel> recentlyPlayedGames2 = new ObservableCollection<GameModel>();
        public ObservableCollection<GameModel> RecentlyPlayedGames2 => recentlyPlayedGames2;

        internal ObservableCollection<GameGroup> recentlyAddedGames = new ObservableCollection<GameGroup>();
        public ObservableCollection<GameGroup> RecentlyAddedGames => recentlyAddedGames;

        internal ObservableCollection<GameModel> recentlyAddedGames2 = new ObservableCollection<GameModel>();
        public ObservableCollection<GameModel> RecentlyAddedGames2 => recentlyAddedGames2;

        internal ObservableObject<string> backgroundImagePath = new ObservableObject<string>();
        public ObservableObject<string> BackgroundImagePath => backgroundImagePath;

        internal ObservableCollection<GameGroup> specialGames = new ObservableCollection<GameGroup>();
        public ObservableCollection<GameGroup> SpecialGames => specialGames;

        internal LandingPageSettingsViewModel settings;
        public LandingPageSettingsViewModel Settings => settings;

        internal Clock clock = new Clock();
        public Clock Clock => clock;

        internal SuccessStory.SuccessStoryViewModel successStory;
        public SuccessStory.SuccessStoryViewModel SuccessStory => successStory;

        internal IPlayniteAPI playniteAPI;
        internal LandingPage plugin;

        public ICommand OpenSettingsCommand => new RelayCommand(() => plugin.OpenSettingsView());
        public LandingPageViewModel(IPlayniteAPI playniteAPI, LandingPage landingPage,
                                    LandingPageSettingsViewModel settings,
                                    SuccessStory.SuccessStoryViewModel successStory)
        {
            this.playniteAPI = playniteAPI;
            this.plugin = landingPage;
            this.settings = settings;
            this.successStory = successStory;
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

        public void Update()
        {
            UpdateRecentlyPlayedGames();
            UpdateRecentlyAddedGames();
            UpdateBackgroundImagePath();
            UpdateMostPlayedGame();
            successStory.Update();
        }

        private void UpdateMostPlayedGame()
        {
            var groups = new List<GameGroup>();
            if (playniteAPI.Database.Games.MaxElement(g => g.Playtime).Value is Game game)
            {
                var group = new GameGroup();
                group.Games.Add(new GameModel(game)
                {
                    OpenCommand = new RelayCommand(() => { playniteAPI.MainView.SwitchToLibraryView(); playniteAPI.MainView.SelectGame(game.Id); }),
                    StartCommand = new RelayCommand(() => { playniteAPI.StartGame(game.Id); })
                });
                group.Label = ResourceProvider.GetString("LOC_SPG_MostPlayedGame");
                groups.Add(group);
            }

            var dateComparer = new Func<Game, Game, int>((Game a, Game b) =>
            {
                if (a?.ReleaseDate == null)
                    return 1;
                if (b?.ReleaseDate == null)
                    return -1;
                return -a.ReleaseDate.Value.CompareTo(b.ReleaseDate.Value);
            });

            if (playniteAPI.Database.Games.MaxElement(dateComparer) is Game newestGame)
            {
                var group = new GameGroup();
                group.Games.Add(new GameModel(newestGame)
                {
                    OpenCommand = new RelayCommand(() => { playniteAPI.MainView.SwitchToLibraryView(); playniteAPI.MainView.SelectGame(newestGame.Id); }),
                    StartCommand = new RelayCommand(() => { playniteAPI.StartGame(newestGame.Id); })
                });
                group.Label = ResourceProvider.GetString("LOC_SPG_LatestReleaseGame");
                groups.Add(group);
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                specialGames.Update(groups);
            });
        }

        private void UpdateBackgroundImagePath()
        {
            var path = recentlyPlayedGames.SelectMany(group => group.Games)
                .Concat(recentlyAddedGames.SelectMany(group => group.Games))
                .FirstOrDefault(g => !string.IsNullOrEmpty(g.Game.BackgroundImage))?.Game.BackgroundImage;
            if (path is string)
            {
                Application.Current.Dispatcher.Invoke(() => 
                {
                    BackgroundImagePath.Object = path;
                });
            }
        }

        internal void UpdateRecentlyPlayedGames()
        {
            var games = playniteAPI.Database.Games
                .Where(g => !g.Hidden)
                .OrderByDescending(g => g.LastActivity);
            //var displayedGames = Math.Min(Math.Max(10, games.Count(g => g.LastActivity?.CompareTo(DateTime.Today.AddDays(-7)) > 0)), 20);
            //var groups = games.Take(displayedGames).GroupBy(g => g.LastActivity.ToGroupName())
            //    .Select(group =>
            //    {
            //        var gameGroup = new GameGroup() { Label = group.Key };
            //        foreach (var game in group)
            //        {
            //            gameGroup.Games.Add(new GameModel(game)
            //            {
            //                OpenCommand = new RelayCommand(() => { playniteAPI.MainView.SwitchToLibraryView(); playniteAPI.MainView.SelectGame(game.Id); }),
            //                StartCommand = new RelayCommand(() => { playniteAPI.StartGame(game.Id); })
            //            });
            //        }
            //        return gameGroup;
            //    });
            Application.Current.Dispatcher.Invoke(() =>
            {
                // recentlyPlayedGames.Update(groups);
                recentlyPlayedGames2.Update(games.Take(10).Select(g => new GameModel(g)));
            });
        }

        internal void UpdateRecentlyAddedGames()
        {
            var games = playniteAPI.Database.Games
                .Where(g => !g.Hidden)
                .OrderByDescending(g => g.Added);
            
            //var displayedGames = Math.Min(Math.Max(10, games.Count(g => g.Added?.CompareTo(DateTime.Today.AddDays(-7)) > 0)), 10);
            //var groups = games.Take(displayedGames).GroupBy(g => g.Added.ToGroupName())
            //    .Select(group =>
            //    {
            //        var gameGroup = new GameGroup() { Label = group.Key };
            //        foreach (var game in group)
            //        {
            //            gameGroup.Games.Add(new GameModel(game)
            //            {
            //                OpenCommand = new RelayCommand(() => { playniteAPI.MainView.SwitchToLibraryView(); playniteAPI.MainView.SelectGame(game.Id); }),
            //                StartCommand = new RelayCommand(() => { playniteAPI.StartGame(game.Id); })
            //            });
            //        }
            //        return gameGroup;
            //    });
            Application.Current.Dispatcher.Invoke(() =>
            {
                // recentlyAddedGames.Update(groups);
                recentlyAddedGames2.Update(games.Take(10).Select(g => new GameModel(g)));
            });
        }

        private void Games_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> e)
        {
            Update();
        }

        private void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
        {
            Update();
        }
    }
}
