using LandingPage.Extensions;
using LandingPage.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using StartPage.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace LandingPage.ViewModels
{
    public class ShelvesViewModel : ObservableObject, IStartPageControl
    {
        internal ObservableCollection<ShelveViewModel> shelveViewModels = new ObservableCollection<ShelveViewModel>();
        public ObservableCollection<ShelveViewModel> ShelveViewModels { get => shelveViewModels; set => SetValue(ref shelveViewModels, value); }

        public ICommand AddShelveCommand { get; set; }

        public ICommand RemoveShelveCommand { get; set; }

        public ICommand ExtendShelveCommand { get; set; }

        public Game LastSelectedGame { get => plugin.startPageViewModel.LastSelectedGame; set => plugin.startPageViewModel.LastSelectedGame = value; }

        public Game LastHoveredGame { get => plugin.startPageViewModel.LastHoveredGame; set => plugin.startPageViewModel.LastHoveredGame = value; }

        public Game CurrentlyHoveredGame
        {
            get => plugin.startPageViewModel.CurrentlyHoveredGame;
            set
            {
                if (value is Game && plugin.startPageViewModel.LastHoveredGame != value)
                {
                    plugin.startPageViewModel.LastHoveredGame = value;
                }
            }
        }

        internal IPlayniteAPI playniteAPI;
        internal LandingPageExtension plugin;

        internal LandingPageSettingsViewModel settings;
        public LandingPageSettingsViewModel Settings => settings;

        public ShelvesViewModel(
            IPlayniteAPI playniteAPI,
            LandingPageExtension landingPage,
            LandingPageSettingsViewModel settings)
        {
            this.playniteAPI = playniteAPI;
            this.plugin = landingPage;
            this.settings = settings;

            Settings.Settings.PropertyChanged += Settings_PropertyChanged;
            Settings.PropertyChanged += Settings_PropertyChanged1;

            foreach (var shelveProperties in settings.Settings.ShelveProperties)
            {
                ShelveViewModels.Add(new ShelveViewModel(shelveProperties, playniteAPI, ShelveViewModels));
            }

            AddShelveCommand = new RelayCommand(() =>
            {
                ShelveViewModel item = new ShelveViewModel(ShelveProperties.RecentlyPlayed, playniteAPI, ShelveViewModels);
                Settings.Settings.ShelveProperties.Add(item.ShelveProperties);
                ShelveViewModels.Add(item);
                ShelveViewModels.ForEach(m => m.UpdateGames(m.ShelveProperties));
            });
            RemoveShelveCommand = new RelayCommand<ShelveViewModel>(svm =>
            {
                if (svm != null)
                {
                    Settings.Settings.ShelveProperties.Remove(svm.ShelveProperties);
                    ShelveViewModels.Remove(svm);
                    ShelveViewModels.ForEach(m => m.UpdateGames(m.ShelveProperties));
                }
            });
            ExtendShelveCommand = new RelayCommand<ShelveViewModel>(svm =>
            {
                var idx = ShelveViewModels.IndexOf(svm);
                if (idx > -1)
                {
                    var properties = svm.ShelveProperties.Copy();
                    properties.SkippedGames = svm.ShelveProperties.NumberOfGames + svm.ShelveProperties.SkippedGames;
                    properties.Name = string.Empty;
                    ShelveViewModel item = new ShelveViewModel(properties, playniteAPI, ShelveViewModels);
                    Settings.Settings.ShelveProperties.Insert(idx + 1, item.ShelveProperties);
                    ShelveViewModels.Insert(idx + 1, item);
                    ShelveViewModels.ForEach(m => m.UpdateGames(m.ShelveProperties));
                }
            });
        }

        private void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
        {
            if (e.UpdatedItems.Any(u => IsRelevantUpdate(u)))
            {
                ShelveViewModels.ForEach(m => m.UpdateGames(m.ShelveProperties));
            }
        }

        private void Games_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> e)
        {
            if (e.RemovedItems.Count + e.AddedItems.Count > 0)
            {
                ShelveViewModels.ForEach(m => m.UpdateGames(m.ShelveProperties));
            }
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

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LandingPageSettings.SkipGamesInPreviousShelves))
            {
                ShelveViewModels.ForEach(m => m.UpdateGames(m.ShelveProperties));
            }
        }

        private void Settings_PropertyChanged1(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LandingPageSettingsViewModel.Settings))
            {
                ShelveViewModels.ForEach(m => m.UpdateGames(m.ShelveProperties));
                Settings.Settings.PropertyChanged += Settings_PropertyChanged;
            }
        }

        public void OnViewRemoved(string viewId, Guid instanceId)
        {

        }

        public void OnStartPageOpened()
        {
            ShelveViewModels.ForEach(m => m.UpdateGames(m.ShelveProperties));
            Subscribe();
        }

        public void OnStartPageClosed()
        {
            Unsubscribe();
        }

        public void OnDayChanged(DateTime newTime)
        {
            ShelveViewModels.ForEach(m => m.UpdateGames(m.ShelveProperties));
        }
    }
}
