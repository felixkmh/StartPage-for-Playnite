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
        internal ObservableCollection<GameModel> recentlyPlayedGames = new ObservableCollection<GameModel>();
        public ObservableCollection<GameModel> RecentlyPlayedGames => recentlyPlayedGames;

        internal ObservableCollection<GameModel> recentlyAddedGames = new ObservableCollection<GameModel>();
        public ObservableCollection<GameModel> RecentlyAddedGames => recentlyAddedGames;

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
        internal LandingPageExtension plugin;

        public ICommand OpenSettingsCommand => new RelayCommand(() => plugin.OpenSettingsView());
        public LandingPageViewModel(IPlayniteAPI playniteAPI, LandingPageExtension landingPage,
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
                group.Games.Add(new GameModel(game));
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
                group.Games.Add(new GameModel(newestGame));
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
            var path = recentlyPlayedGames
                .Concat(recentlyAddedGames)
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

            Application.Current.Dispatcher.Invoke(() =>
            {
                var displayedGames = Math.Min(Math.Max(10, games.Count(g => g.LastActivity?.CompareTo(DateTime.Today.AddDays(-7)) > 0)), 20);
                int i = 0;
                foreach (var game in games.Take(displayedGames))
                {
                    if (recentlyPlayedGames.Count > i)
                    {
                        recentlyPlayedGames[i].Game = game;
                    }
                    else
                    {
                        recentlyPlayedGames.Add(new GameModel(game));
                    }
                    ++i;
                }
                for (int j = recentlyPlayedGames.Count - 1; j >= i; --j)
                {
                    recentlyPlayedGames.RemoveAt(j);
                }
            });
        }

        internal void UpdateRecentlyAddedGames()
        {
            var games = playniteAPI.Database.Games
                .Where(g => !g.Hidden)
                .OrderByDescending(g => g.Added);
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                var displayedGames = Math.Min(Math.Max(10, games.Count(g => g.Added?.CompareTo(DateTime.Today.AddDays(-7)) > 0)), 20);
                int i = 0;
                foreach (var game in games.Take(displayedGames))
                {
                    if (recentlyAddedGames.Count > i)
                    {
                        recentlyAddedGames[i].Game = game;
                    }
                    else
                    {
                        recentlyAddedGames.Add(new GameModel(game));
                    }
                    ++i;
                }
                for (int j = recentlyAddedGames.Count - 1; j >= i; --j)
                {
                    recentlyAddedGames.RemoveAt(j);
                }
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
