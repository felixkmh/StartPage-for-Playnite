using LandingPage.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace LandingPage.ViewModels
{
    public class ShelveViewModel : BusyObservableObject, IStartPageViewModel
    {
        private IPlayniteAPI playniteAPI;

        private static Random random = new Random();

        private ShelveProperties shelveProperties = ShelveProperties.RecentlyPlayed;
        public ShelveProperties ShelveProperties { get => shelveProperties; set => SetValue(ref shelveProperties, value); }

        private LandingPage.Settings.ShelvesSettings shelveSettings = new LandingPage.Settings.ShelvesSettings();
        public LandingPage.Settings.ShelvesSettings ShelveSettings { get => shelveSettings; set => SetValue(ref shelveSettings, value); }

        private CollectionViewSource collectionViewSource = new CollectionViewSource();
        public CollectionViewSource CollectionViewSource { get => collectionViewSource; set => SetValue(ref collectionViewSource, value); }

        private ObservableCollection<GameModel> games = new ObservableCollection<GameModel>();
        public ObservableCollection<GameModel> Games { get => games; set => SetValue(ref games, value); }

        private ICommand resetFiltersCommand;
        public ICommand ResetFiltersCommand { get => resetFiltersCommand; set => SetValue(ref resetFiltersCommand, value); }

        private ICommand manualUpdateCommand;
        public ICommand ManualUpdateCommand { get => manualUpdateCommand; set => SetValue(ref manualUpdateCommand, value); }

        private ObservableCollection<ShelveViewModel> viewModels;
        private readonly Game DummyGame = new Game();

        public ShelveViewModel(ShelveProperties shelveProperties, LandingPage.Settings.ShelvesSettings shelveSettings, IPlayniteAPI playniteAPI, ObservableCollection<ShelveViewModel> viewModels)
        {
            this.viewModels = viewModels;
            collectionViewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription());
            UpdateOrder(shelveProperties, collectionViewSource);
            UpdateSorting(shelveProperties, collectionViewSource);
            UpdateGrouping(shelveProperties, collectionViewSource);
            collectionViewSource.IsLiveSortingRequested = true;
            collectionViewSource.IsLiveGroupingRequested = true;
            collectionViewSource.Source = Games;
            this.playniteAPI = playniteAPI;
            this.shelveProperties = shelveProperties;
            this.shelveSettings = shelveSettings;
            // UpdateGames(shelveProperties);
            this.shelveProperties.PropertyChanged += ShelveProperties_PropertyChangedAsync;
            PropertyChanged += ShelveViewModel_PropertyChangedAsync;
            resetFiltersCommand = new RelayCommand(async () =>
            {
                ShelveProperties.Categories.Clear();
                ShelveProperties.Tags.Clear();
                ShelveProperties.Genres.Clear();
                ShelveProperties.Platforms.Clear();
                ShelveProperties.Sources.Clear();
                ShelveProperties.Features.Clear();
                ShelveProperties.CompletionStatus.Clear();
                OnPropertyChanged(nameof(Categories));
                OnPropertyChanged(nameof(Tags));
                OnPropertyChanged(nameof(Genres));
                OnPropertyChanged(nameof(Platforms));
                OnPropertyChanged(nameof(Sources));
                OnPropertyChanged(nameof(Features));
                OnPropertyChanged(nameof(CompletionStatus));
                await UpdateGamesAsync(ShelveProperties, true);
            });
            manualUpdateCommand = new RelayCommand(async () => await UpdateGamesAsync(ShelveProperties, true));
        }

        private async void ShelveViewModel_PropertyChangedAsync(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ShelveProperties))
            {
                ShelveProperties.PropertyChanged += ShelveProperties_PropertyChangedAsync;
                UpdateOrder(ShelveProperties, CollectionViewSource);
                UpdateSorting(ShelveProperties, CollectionViewSource);
                UpdateGrouping(ShelveProperties, CollectionViewSource);
                await UpdateGamesAsync(ShelveProperties, true);
            }
        }

        private async void ShelveProperties_PropertyChangedAsync(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is ShelveProperties shelveProperties)
            {
                var cvs = CollectionViewSource;
                if (e.PropertyName == nameof(ShelveProperties.Order))
                {
                    UpdateOrder(shelveProperties, cvs);
                    UpdateGrouping(shelveProperties, cvs);
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
                if (e.PropertyName != nameof(ShelveProperties.Name))
                {
                    await UpdateGamesAsync(shelveProperties, true);
                }
            }
        }

        public class ToggleFilter : ObservableObject
        {
            public ToggleFilter(Guid id, string name, HashSet<Guid> source, ShelveViewModel viewModel, bool isChecked = false)
            {
                ViewModel = viewModel;
                Source = source;
                this.id = id;
                this.name = name;
                this.isChecked = isChecked;
            }

            private Guid id;
            public Guid Id { get => id; set => SetValue(ref id, value); }
            private string name;
            public string Name { get => name; set => SetValue(ref name, value); }
            private bool isChecked;
            public bool IsChecked { get => isChecked;
                set
                {
                    var oldValue = isChecked;
                    SetValue(ref isChecked, value);
                    if (value)
                    {
                        Source.Add(id);
                    } else
                    {
                        Source.Remove(id);
                    }
                    if (oldValue != isChecked)
                    {
                        ViewModel.UpdateGames(ViewModel.ShelveProperties, true);
                    }
                }
            }

            public HashSet<Guid> Source { get; private set; }
            public ShelveViewModel ViewModel { get; private set; }
        }

        public IEnumerable<ToggleFilter> Categories => playniteAPI.Database.Categories
                .Select(item => new ToggleFilter(item.Id, item.Name, shelveProperties.Categories, this, shelveProperties.Categories?.Contains(item.Id) ?? false)).OrderByDescending(f => f.IsChecked).ThenBy(f => f.Name);

        public IEnumerable<ToggleFilter> Tags => playniteAPI.Database.Tags
                .Select(item => new ToggleFilter(item.Id, item.Name, shelveProperties.Tags, this, shelveProperties.Tags?.Contains(item.Id) ?? false)).OrderByDescending(f => f.IsChecked).ThenBy(f => f.Name);

        public IEnumerable<ToggleFilter> CompletionStatus => playniteAPI.Database.CompletionStatuses
                .Select(item => new ToggleFilter(item.Id, item.Name, shelveProperties.CompletionStatus, this, shelveProperties.CompletionStatus?.Contains(item.Id) ?? false)).OrderByDescending(f => f.IsChecked).ThenBy(f => f.Name);

        public IEnumerable<ToggleFilter> Features => playniteAPI.Database.Features
                .Select(item => new ToggleFilter(item.Id, item.Name, shelveProperties.Features, this, shelveProperties.Features?.Contains(item.Id) ?? false)).OrderByDescending(f => f.IsChecked).ThenBy(f => f.Name);

        public IEnumerable<ToggleFilter> Platforms => playniteAPI.Database.Platforms
                .Select(item => new ToggleFilter(item.Id, item.Name, shelveProperties.Platforms, this, shelveProperties.Platforms?.Contains(item.Id) ?? false)).OrderByDescending(f => f.IsChecked).ThenBy(f => f.Name);

        public IEnumerable<ToggleFilter> Sources => playniteAPI.Database.Sources
                .Select(item => new ToggleFilter(item.Id, item.Name, shelveProperties.Sources, this, shelveProperties.Sources?.Contains(item.Id) ?? false)).OrderByDescending(f => f.IsChecked).ThenBy(f => f.Name);

        public IEnumerable<ToggleFilter> Genres => playniteAPI.Database.Genres
                .Select(item => new ToggleFilter(item.Id, item.Name, shelveProperties.Genres, this, shelveProperties.Genres?.Contains(item.Id) ?? false)).OrderByDescending(f => f.IsChecked).ThenBy(f => f.Name);

        internal class ScoreGroupComparer : IComparer, IComparer<ScoreGroup>
        {
            public readonly static ScoreGroupComparer Ascending = new ScoreGroupComparer(false);
            public readonly static ScoreGroupComparer Descending = new ScoreGroupComparer(true);

            private ScoreGroupComparer(bool descending)
            {
                multiplier = descending ? -1 : 1;
            }

            private int multiplier = 1;

            public int Compare(object x, object y)
            {
                if (x is CollectionViewGroup xView && y is CollectionViewGroup yView)
                {
                    if (xView.Name is ScoreGroup xGroup && yView.Name is ScoreGroup yGroup)
                    {
                        return Compare(xGroup, yGroup);
                    }
                }
                return 0;
            }

            public int Compare(ScoreGroup x, ScoreGroup y)
            {
                if (x != ScoreGroup.None && y != ScoreGroup.None)
                {
                    var xScore = (int)x + 1;
                    var yScore = (int)y + 1;
                    return multiplier * xScore.CompareTo(yScore);
                }
                if (x == ScoreGroup.None && y != ScoreGroup.None) return 1;
                if (y == ScoreGroup.None && x != ScoreGroup.None) return -1;
                return 0;
            }
        }

        internal class ScoreComparer : IComparer, IComparer<int?>
        {
            public readonly static ScoreComparer Ascending = new ScoreComparer(false);
            public readonly static ScoreComparer Descending = new ScoreComparer(true);

            private ScoreComparer(bool descending)
            {
                multiplier = descending ? -1 : 1;
            }

            private int multiplier = 1;

            public int Compare(object x, object y)
            {
                return Compare((int?)x, (int?)x);
            }

            public int Compare(int? x, int? y)
            {
                if (x is int xScore && y is int yScore)
                {
                    return multiplier * xScore.CompareTo(yScore);
                }
                if (x == null && y != null) return 1;
                if (y == null && x != null) return -1;
                return 0;
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
                case GroupingField.CriticScore:
                    {
                        newGroupDescription.PropertyName = "Game.CriticScoreGroup";
                        //var newSortDescription = new SortDescription { PropertyName = "Name" };
                        //newGroupDescription.SortDescriptions.Add(newSortDescription);
                        newGroupDescription.CustomSort = ScoreGroupComparer.Descending;
                        groupDescriptions.Add(newGroupDescription);
                        break;
                    }
                case GroupingField.UserScore:
                    {
                        newGroupDescription.PropertyName = "Game.UserScoreGroup";
                        //var newSortDescription = new SortDescription { PropertyName = "Name" };
                        //newGroupDescription.SortDescriptions.Add(newSortDescription);
                        newGroupDescription.CustomSort = ScoreGroupComparer.Descending;
                        groupDescriptions.Add(newGroupDescription);
                        break;
                    }
                case GroupingField.CommunityScore:
                    {
                        newGroupDescription.PropertyName = "Game.CommunityScoreGroup";
                        //var newSortDescription = new SortDescription { PropertyName = "Name" };
                        //newGroupDescription.SortDescriptions.Add(newSortDescription);
                        newGroupDescription.CustomSort = ScoreGroupComparer.Descending;
                        groupDescriptions.Add(newGroupDescription);
                        break;
                    }
                case GroupingField.Library:
                    {
                        newGroupDescription.PropertyName = "Source";
                        var newSortDescription = new SortDescription { PropertyName = "Name" };
                        newGroupDescription.SortDescriptions.Add(newSortDescription);
                        groupDescriptions.Add(newGroupDescription);
                        break;
                    }
                case GroupingField.CompletionStatus:
                    {
                        newGroupDescription.PropertyName = "Game.CompletionStatus";
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
            var sortDescription = sortDescriptions.FirstOrDefault();
            sortDescriptions?.Clear();
            var newSortDescription = new SortDescription { Direction = shelveProperties.Order == Order.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending };
            switch (shelveProperties.SortBy)
            {
                case SortingField.Name:
                    newSortDescription.PropertyName = "Game.Name";
                    break;
                case SortingField.SortingName:
                    newSortDescription.PropertyName = "SortingName";
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
                case SortingField.UserScore:
                    newSortDescription.PropertyName = "Game.UserScore";
                    break;
                case SortingField.CommunityScore:
                    newSortDescription.PropertyName = "Game.CommunityScore";
                    break;
                case SortingField.CriticScore:
                    newSortDescription.PropertyName = "Game.CriticScore";
                    break;
                case SortingField.ReleaseDate:
                    newSortDescription.PropertyName = "Game.ReleaseDate";
                    break;
                case SortingField.CompletionStatus:
                    newSortDescription.PropertyName = "Game.CompletionStatus";
                    break;
                default:
                    break;
            }
            if (shelveProperties.SortBy != SortingField.Random)
            {
                if (sortDescription == null)
                {
                    sortDescriptions.Add(newSortDescription);
                }
                sortDescriptions.Add(newSortDescription);
            }
        }

        private static void UpdateOrder(ShelveProperties shelveProperties, CollectionViewSource cvs)
        {
            var sortDescriptions = cvs.SortDescriptions;
            SortDescription sortDescription = sortDescriptions.FirstOrDefault();
            sortDescriptions?.Clear();
            if (sortDescription != null && sortDescription.PropertyName != null)
            {
                sortDescriptions?.Add(new SortDescription { Direction = shelveProperties.Order == Order.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending, PropertyName = sortDescription.PropertyName });
            }
        }

        internal IEnumerable<T> OrderBy<T, U>(IEnumerable<T> enumerable, Func<T, U> func, Order order = Order.Descending)
        {
            if (order == Order.Descending)
            {
                return enumerable.OrderByDescending(func);
            }
            return enumerable.OrderBy(func);
        }

        public async Task UpdateGamesAsync(ShelveProperties shelveProperties, bool manualUpdate = false)
        {
            var needsUpdate = manualUpdate;
            needsUpdate |= Games.Count == 0;
            needsUpdate |= ShelveProperties.SortBy != SortingField.Random;
            if (Games.Any(g => playniteAPI.Database.Games.Get(g.Game.Id) == null))
            {
                needsUpdate |= true;
            }

            if (!needsUpdate)
                return;

            IsBusy = true;
            var gameSelection = await GetGamesAsync(shelveProperties);

            var collection = Games;
            var changed = false;

            using (var defer = CollectionViewSource.DeferRefresh())
            {
                foreach (var game in gameSelection)
                {
                    if (collection.FirstOrDefault(item => item.Game?.Id == game.Id) is GameModel model)
                    {
                        if (model.Game.LastActivity != game.LastActivity)
                        {
                            changed = true;
                        }
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
                    collection.Move(collection.Count - 1, 0);
                    GC.Collect();
                }
            }
            IsBusy = false;
        }

        public void UpdateGames(ShelveProperties shelveProperties, bool manualUpdate = false)
        {
            var needsUpdate = manualUpdate;
            needsUpdate |= Games.Count == 0;
            needsUpdate |= ShelveProperties.SortBy != SortingField.Random;
            if (Games.Any(g => playniteAPI.Database.Games.Get(g.Game.Id) == null))
            {
                needsUpdate |= true;
            }

            if (!needsUpdate)
                return;

            IsBusy = true;
            IEnumerable<Game> gameSelection = GetGames(shelveProperties);

            var collection = Games;
            var changed = false;

            Application.Current.Dispatcher.Invoke(() =>
            {
                using (var defer = CollectionViewSource.DeferRefresh())
                {
                    foreach (var game in gameSelection)
                    {
                        if (collection.FirstOrDefault(item => item.Game?.Id == game.Id) is GameModel model)
                        {
                            if (model.Game.LastActivity != game.LastActivity)
                            {
                                changed = true;
                            }
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
                        collection.Move(collection.Count - 1, 0);
                        GC.Collect();
                    }
                }
            });
            IsBusy = false;
        }

        private async Task<List<Game>> GetGamesAsync(ShelveProperties shelveProperties)
        {
            return await Task.Run(() => GetGames(shelveProperties));
        }

        private List<Game> GetGames(ShelveProperties shelveProperties)
        {
            IEnumerable<Game> games = playniteAPI.Database.Games
                                    .Where(g => (!g.TagIds?.Contains(LandingPageExtension.Instance.SettingsViewModel.Settings.IgnoreTagId)) ?? true)
                                    .Where(g => g.Favorite || !shelveProperties.FavoritesOnly)
                                    .Where(g => !g.Hidden || !shelveProperties.IgnoreHidden)
                                    .Where(g => g.IsInstalled || !shelveProperties.InstalledOnly);
            // apply filters
            if (shelveProperties.Categories?.Any() ?? false)
                games = games.Where(g => g.CategoryIds?.Any(id => shelveProperties.Categories.Contains(id)) ?? false);
            if (shelveProperties.Genres?.Any() ?? false)
                games = games.Where(g => g.GenreIds?.Any(id => shelveProperties.Genres.Contains(id)) ?? false);
            if (shelveProperties.Tags?.Any() ?? false)
                games = games.Where(g => g.TagIds?.Any(id => shelveProperties.Tags.Contains(id)) ?? false);
            if (shelveProperties.CompletionStatus?.Any() ?? false)
                games = games.Where(g => shelveProperties.CompletionStatus.Contains(g.CompletionStatusId));
            if (shelveProperties.Features?.Any() ?? false)
                games = games.Where(g => g.FeatureIds?.Any(id => shelveProperties.Features.Contains(id)) ?? false);
            if (shelveProperties.Sources?.Any() ?? false)
                games = games.Where(g => shelveProperties.Sources.Contains(g.SourceId));
            if (shelveProperties.Platforms?.Any() ?? false)
                games = games.Where(g => g.PlatformIds?.Any(id => shelveProperties.Platforms.Contains(id)) ?? false);

            if (LandingPageExtension.Instance.SettingsViewModel.Settings.SkipGamesInPreviousShelves)
            {
                var current = viewModels.IndexOf(this);
                if (current > -1)
                {
                    for (var i = current - 1; i >= 0; --i)
                    {
                        ShelveViewModel shelveViewModel = viewModels[i];
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            games = games.Where(g => !shelveViewModel.Games.Any(m => m.Game.Id == g.Id));
                        });
                    }
                }
            }

            switch (shelveProperties.SortBy)
            {
                case SortingField.Name:
                    games = OrderBy(games, g => g.Name, shelveProperties.Order);
                    break;
                case SortingField.SortingName:
                    games = OrderBy(games, g => string.IsNullOrEmpty(g.SortingName) ? g.Name : g.SortingName, shelveProperties.Order);
                    break;
                case SortingField.LastActivity:
                    games = OrderBy(games, g => g.LastActivity, shelveProperties.Order);
                    break;
                case SortingField.DateAdded:
                    games = OrderBy(games, g => g.Added, shelveProperties.Order);
                    break;
                case SortingField.Playtime:
                    games = OrderBy(games, g => g.Playtime, shelveProperties.Order);
                    break;
                case SortingField.ReleaseDate:
                    games = OrderBy(games, g => g.ReleaseDate, shelveProperties.Order);
                    break;
                case SortingField.UserScore:
                    games = games.OrderBy(g => g.UserScore, shelveProperties.Order == Order.Ascending ? ScoreComparer.Ascending : ScoreComparer.Descending);
                    break;
                case SortingField.CommunityScore:
                    games = games.OrderBy(g => g.CommunityScore, shelveProperties.Order == Order.Ascending ? ScoreComparer.Ascending : ScoreComparer.Descending);
                    break;
                case SortingField.CriticScore:
                    games = games.OrderBy(g => g.CriticScore, shelveProperties.Order == Order.Ascending ? ScoreComparer.Ascending : ScoreComparer.Descending);
                    break;
                case SortingField.CompletionStatus:
                    games = OrderBy(games, g => g.CompletionStatus, shelveProperties.Order);
                    break;
                case SortingField.Random:
                    games = games.OrderBy(g => random.Next());
                    break;
                default:
                    break;
            }

            var skipped = shelveProperties.SkippedGames;

            if (LandingPageExtension.Instance.SettingsViewModel.Settings.SkipGamesInPreviousShelves)
            {
                skipped = 0;
            }

            List<Game> gameSelection = games.Skip(skipped).Take(shelveProperties.NumberOfGames).ToList();
            if (!gameSelection.Any())
            {
                var dummies = new List<Game>();
                dummies.Add(DummyGame);
                gameSelection = dummies;
            }

            return gameSelection;
        }

        public void OnViewClosed()
        {
            ShelveProperties.PropertyChanged -= ShelveProperties_PropertyChangedAsync;
            PropertyChanged -= ShelveViewModel_PropertyChangedAsync;
        }
    }
}
