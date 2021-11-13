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
using System.Collections;

namespace LandingPage.ViewModels
{

    public class LandingPageViewModel : ObservableObject
    {
        internal ObservableCollection<GameModel> recentlyPlayedGames = new ObservableCollection<GameModel>();
        public ObservableCollection<GameModel> RecentlyPlayedGames => recentlyPlayedGames;

        internal ObservableCollection<GameModel> recentlyAddedGames = new ObservableCollection<GameModel>();
        public ObservableCollection<GameModel> RecentlyAddedGames => recentlyAddedGames;

        internal ObservableCollection<GameModel> favoriteGames = new ObservableCollection<GameModel>();
        public ObservableCollection<GameModel> FavoriteGames => favoriteGames;

        internal ObservableObject<string> backgroundImagePath = new ObservableObject<string>();
        public ObservableObject<string> BackgroundImagePath => backgroundImagePath;

        internal ObservableCollection<GameGroup> specialGames = new ObservableCollection<GameGroup>();
        public ObservableCollection<GameGroup> SpecialGames => specialGames;

        internal ObservableCollection<NotificationMessage> notifications;
        public ObservableCollection<NotificationMessage> Notifications => notifications;

        internal ICommand deleteNotificationCommand;
        public ICommand DeleteNotificationCommand => deleteNotificationCommand;

        internal ICommand clearNotificationsCommand;
        public ICommand ClearNotificationsCommand => clearNotificationsCommand;

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
            notifications = playniteAPI.Notifications.Messages;
            notifications.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(Notifications));
            deleteNotificationCommand = new RelayCommand<NotificationMessage>(sender => playniteAPI.Notifications.Remove(sender.Id));
            clearNotificationsCommand = new RelayCommand(() => playniteAPI.Notifications.RemoveAll());
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
            UpdateFavorites();
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
                if (specialGames.Count == 0)
                {
                    specialGames.Update(groups);
                } else
                {
                    foreach(var group in specialGames)
                    {
                        var currentGroup = groups.FirstOrDefault(g => g.Label == group.Label);
                        group.Games.FirstOrDefault().Game = currentGroup.Games.FirstOrDefault().Game;
                    }
                }
            });
        }

        private void UpdateBackgroundImagePath()
        {
            var path = recentlyPlayedGames.OrderByDescending(g => g.Game.LastActivity)
                .Concat(recentlyAddedGames.OrderByDescending(g => g.Game.Added))
                .FirstOrDefault(g => !string.IsNullOrEmpty(g.Game.BackgroundImage))?.Game.BackgroundImage;
            if (string.IsNullOrEmpty(path))
            {

            }
            if (path is string)
            {
                Application.Current.Dispatcher.Invoke(() => 
                {
                    BackgroundImagePath.Object = path;
                });
            }
        }

        GameModel dummy = new GameModel(new Game());

        internal void UpdateRecentlyPlayedGames()
        {
            var games = playniteAPI.Database.Games
                .Where(g => !g.Hidden)
                .OrderByDescending(g => g.LastActivity);
            var collection = recentlyPlayedGames;
            var changed = false;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var displayedGames = Math.Min(Math.Max(10, games.Count(g => g.LastActivity?.CompareTo(DateTime.Today.AddDays(-7)) > 0)), 20);
                
                IEnumerable<Game> gameSelection = games.Take(displayedGames);
                foreach (var game in gameSelection)
                {
                    if (collection.FirstOrDefault(item => item.Game?.Id == game.Id) is GameModel model)
                    {
                        if (model.Game.LastActivity != game.LastActivity)
                        {
                            changed = true;
                        }
                        model.Game = game;
                    }
                    else if (collection.FirstOrDefault(item => gameSelection.All(s => s.Id != item.Game?.Id)) is GameModel unusedModel)
                    {
                        changed = true;
                        collection.Remove(unusedModel);
                        unusedModel.Game = game;
                        collection.Add(unusedModel);
                    }
                    else
                    {
                        changed = true;
                        collection.Add(new GameModel(game));
                    }
                }
                for (int j = collection.Count - 1; j >= 0; --j)
                {
                    if (gameSelection.All(g => g.Id != collection[j].Game?.Id))
                    {
                        changed = true;
                        collection.RemoveAt(j);
                    }
                }
                if (changed && collection.Count > 1)
                {
                    collection.Move(collection.Count - 1, 0);
                }
            });
        }

        internal void UpdateRecentlyAddedGames()
        {
            var games = playniteAPI.Database.Games
                .Where(g => !g.Hidden)
                .OrderByDescending(g => g.Added);
            var collection = recentlyAddedGames;
            var changed = false;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var displayedGames = Math.Min(Math.Max(10, games.Count(g => g.Added?.CompareTo(DateTime.Today.AddDays(-7)) > 0)), 20);
                
                IEnumerable<Game> gameSelection = games.Take(displayedGames);
                foreach (var game in gameSelection)
                {
                    if (collection.FirstOrDefault(item => item.Game?.Id == game.Id) is GameModel model)
                    {
                        if (model.Game.Added != game.Added)
                        {
                            changed = true;
                        }
                        model.Game = game;
                    } else if (collection.FirstOrDefault(item => gameSelection.All(s => s.Id != item.Game?.Id)) is GameModel unusedModel)
                    {
                        changed = true;
                        collection.Remove(unusedModel);
                        unusedModel.Game = game;
                        collection.Add(unusedModel);
                    } else
                    {
                        changed = true;
                        collection.Add(new GameModel(game));
                    }
                }
                for (int j = collection.Count - 1; j >= 0; --j)
                {
                    if (gameSelection.All(g => g.Id != collection[j].Game?.Id))
                    {
                        changed = true;
                        collection.RemoveAt(j);
                    }
                }
                if (changed && collection.Count > 1)
                {
                    collection.Move(collection.Count - 1, 0);
                }
            });
        }

        internal void UpdateFavorites()
        {
            var games = playniteAPI.Database.Games
                .Where(g => !g.Hidden && g.Favorite)
                .OrderByDescending(g => g.LastActivity);
            var collection = favoriteGames;
            var changed = false;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var displayedGames = 20;

                IEnumerable<Game> gameSelection = games.Take(displayedGames);
                foreach (var game in gameSelection)
                {
                    if (collection.FirstOrDefault(item => item.Game?.Id == game.Id) is GameModel model)
                    {
                        if (model.Game.LastActivity != game.LastActivity)
                        {
                            changed = true;
                        }
                        model.Game = game;
                    }
                    else if (collection.FirstOrDefault(item => gameSelection.All(s => s.Id != item.Game?.Id)) is GameModel unusedModel)
                    {
                        changed = true;
                        collection.Remove(unusedModel);
                        unusedModel.Game = game;
                        collection.Add(unusedModel);
                    }
                    else
                    {
                        changed = true;
                        collection.Add(new GameModel(game));
                    }
                }
                for (int j = collection.Count - 1; j >= 0; --j)
                {
                    if (gameSelection.All(g => g.Id != collection[j].Game?.Id))
                    {
                        changed = true;
                        collection.RemoveAt(j);
                    }
                }
                if (changed && collection.Count > 1)
                {
                    collection.Move(collection.Count - 1, 0);
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
