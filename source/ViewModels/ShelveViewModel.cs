using LandingPage.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LandingPage.ViewModels
{
    public class ShelveViewModel : ObservableObject
    {
        private IPlayniteAPI playniteAPI;

        private ShelveProperties shelveProperties = ShelveProperties.RecentlyPlayed;
        public ShelveProperties ShelveProperties { get => shelveProperties; set => SetValue(ref shelveProperties, value); }

        private CollectionViewSource collectionViewSource = new CollectionViewSource();
        public CollectionViewSource CollectionViewSource { get => collectionViewSource; set => SetValue(ref collectionViewSource, value); }

        private ObservableCollection<GameModel> games = new ObservableCollection<GameModel>();
        public ObservableCollection<GameModel> Games { get => games; set => SetValue(ref games, value); }

        public ShelveViewModel(ShelveProperties shelveProperties, IPlayniteAPI playniteAPI)
        {
            collectionViewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription());
            UpdateOrder(shelveProperties, collectionViewSource);
            UpdateSorting(shelveProperties, collectionViewSource);
            UpdateGrouping(shelveProperties, collectionViewSource);
            collectionViewSource.IsLiveSortingRequested = true;
            collectionViewSource.IsLiveGroupingRequested = true;
            collectionViewSource.Source = Games;
            this.playniteAPI = playniteAPI;
            this.shelveProperties = shelveProperties;
            UpdateGames(shelveProperties);
            this.shelveProperties.PropertyChanged += ShelveProperties_PropertyChanged;
            PropertyChanged += ShelveViewModel_PropertyChanged;
        }

        private void ShelveViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ShelveProperties))
            {
                UpdateOrder(ShelveProperties, CollectionViewSource);
                UpdateSorting(ShelveProperties, CollectionViewSource);
                UpdateGrouping(ShelveProperties, CollectionViewSource);
                UpdateGames(ShelveProperties);
            }
        }

        private void ShelveProperties_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is ShelveProperties shelveProperties)
            {
                var cvs = CollectionViewSource;
                if (e.PropertyName == nameof(ShelveProperties.Order))
                {
                    UpdateOrder(shelveProperties, cvs);
                }
                if (e.PropertyName == nameof(ShelveProperties.SortBy))
                {
                    UpdateSorting(shelveProperties, cvs);
                }
                if (e.PropertyName == nameof(ShelveProperties.GroupBy))
                {
                    UpdateGrouping(shelveProperties, cvs);
                }
                if (e.PropertyName == nameof(ShelveProperties.InstalledOnly)
                    || e.PropertyName == nameof(ShelveProperties.IgnoreHidden)
                    || e.PropertyName == nameof(ShelveProperties.NumberOfGames))
                {
                    
                }
                UpdateGames(shelveProperties);
            }
        }

        private static void UpdateGrouping(ShelveProperties shelveProperties, CollectionViewSource cvs)
        {
            var groupDescriptions = cvs.GroupDescriptions;
            var groupDescription = groupDescriptions.FirstOrDefault() ?? new PropertyGroupDescription();
            groupDescriptions.Clear();
            var newGroupDescription = new PropertyGroupDescription();
            switch (shelveProperties.GroupBy)
            {
                case GroupingField.None:
                    {
                        break;
                    }
                case GroupingField.LastActivity:
                    {
                        newGroupDescription.PropertyName = "Game.LastActivity.Date";
                        var newSortDescription = new SortDescription { Direction = (ListSortDirection)shelveProperties.Order, PropertyName = "Name" };
                        newGroupDescription.SortDescriptions.Add(newSortDescription);
                        groupDescriptions.Add(newGroupDescription);
                        break;
                    }
                case GroupingField.DateAdded:
                    {
                        newGroupDescription.PropertyName = "Game.Added.Date";
                        var newSortDescription = new SortDescription { Direction = (ListSortDirection)shelveProperties.Order, PropertyName = "Name" };
                        newGroupDescription.SortDescriptions.Add(newSortDescription);
                        groupDescriptions.Add(newGroupDescription);
                        break;
                    }
                default:
                    break;
            }
            
        }

        private static void UpdateSorting(ShelveProperties shelveProperties, CollectionViewSource cvs)
        {
            var sortDescriptions = cvs.SortDescriptions;
            var sortDescription = sortDescriptions.First();
            sortDescriptions.Clear();
            var newSortDescription = new SortDescription { Direction = sortDescription.Direction };
            switch (shelveProperties.SortBy)
            {
                case SortingField.Name:
                    newSortDescription.PropertyName = "Game.Name";
                    break;
                case SortingField.SortingName:
                    newSortDescription.PropertyName = "Game.SortingName";
                    break;
                case SortingField.LastActivity:
                    newSortDescription.PropertyName = "Game.LastActivity";
                    break;
                case SortingField.DateAdded:
                    newSortDescription.PropertyName = "Game.Added";
                    break;
                case SortingField.Playtime:
                    newSortDescription.PropertyName = "Game.Playtime";
                    break;
                default:
                    break;
            }
            sortDescriptions.Add(newSortDescription);
        }

        private static void UpdateOrder(ShelveProperties shelveProperties, CollectionViewSource cvs)
        {
            var sortDescriptions = cvs.SortDescriptions;
            var sortDescription = sortDescriptions.First();
            sortDescriptions.Clear();
            sortDescriptions.Add(new SortDescription { Direction = (ListSortDirection)shelveProperties.Order, PropertyName = sortDescription.PropertyName });
        }

        public void UpdateGames(ShelveProperties shelveProperties)
        {
            IEnumerable<Game> games = playniteAPI.Database.Games
                            .Where(g => g.Favorite || !shelveProperties.FavoritesOnly)
                            .Where(g => !g.Hidden || !shelveProperties.IgnoreHidden)
                            .Where(g => g.IsInstalled || !shelveProperties.InstalledOnly);

            if (shelveProperties.Order == Order.Ascending)
            {
                switch (shelveProperties.SortBy)
                {
                    case SortingField.Name:
                        games = games.OrderBy(g => g.Name);
                        break;
                    case SortingField.SortingName:
                        games = games.OrderBy(g => g.SortingName);
                        break;
                    case SortingField.LastActivity:
                        games = games.OrderBy(g => g.LastActivity);
                        break;
                    case SortingField.DateAdded:
                        games = games.OrderBy(g => g.Added);
                        break;
                    case SortingField.Playtime:
                        games = games.OrderBy(g => g.Playtime);
                        break;
                    case SortingField.ReleaseDate:
                        games = games.OrderBy(g => g.ReleaseDate);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                switch (shelveProperties.SortBy)
                {
                    case SortingField.Name:
                        games = games.OrderByDescending(g => g.Name);
                        break;
                    case SortingField.SortingName:
                        games = games.OrderByDescending(g => g.SortingName);
                        break;
                    case SortingField.LastActivity:
                        games = games.OrderByDescending(g => g.LastActivity);
                        break;
                    case SortingField.DateAdded:
                        games = games.OrderByDescending(g => g.Added);
                        break;
                    case SortingField.Playtime:
                        games = games.OrderByDescending(g => g.Playtime);
                        break;
                    case SortingField.ReleaseDate:
                        games = games.OrderByDescending(g => g.ReleaseDate);
                        break;
                    default:
                        break;
                }
            }
            var collection = Games;
            var changed = false;

            IEnumerable<Game> gameSelection = games.Skip(shelveProperties.SkippedGames).Take(shelveProperties.NumberOfGames);
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
        }
    }
}
