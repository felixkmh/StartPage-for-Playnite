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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace LandingPage.ViewModels
{
    public class ShelvesViewModel : ObservableObject, IStartPageControl, IStartPageViewModel
    {
        internal ObservableCollection<ShelveViewModel> shelveViewModels = new ObservableCollection<ShelveViewModel>();
        public ObservableCollection<ShelveViewModel> ShelveViewModels { get => shelveViewModels; set => SetValue(ref shelveViewModels, value); }

        public ICommand AddShelveCommand { get; set; }

        public ICommand RemoveShelveCommand { get; set; }

        public ICommand ExtendShelveCommand { get; set; }

        public ICommand MoveShelveUpCommand { get; set; }
        public ICommand MoveShelveDownCommand { get; set;}

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

        internal DispatcherTimer dispatcherTimer;

        private Guid instanceId;
        public Guid InstanceId { get => instanceId; set => SetValue(ref instanceId, value); }

        private ObservableCollection<ShelveProperties> shelves;
        public ObservableCollection<ShelveProperties> Shelves { get => shelves; set => SetValue(ref shelves, value); }

        public ShelvesViewModel(
            IPlayniteAPI playniteAPI,
            LandingPageExtension landingPage,
            LandingPageSettingsViewModel settings,
            Guid instanceId)
        {
            this.playniteAPI = playniteAPI;
            this.plugin = landingPage;
            this.settings = settings;
            this.instanceId = instanceId;

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(250);

            Settings.Settings.PropertyChanged += Settings_PropertyChanged;
            Settings.PropertyChanged += Settings_PropertyChanged1;

            if (!Settings.Settings.ShelveInstances.ContainsKey(InstanceId))
            {
                if (Settings.Settings.ShelveInstances.Count == 0)
                {
                    Settings.Settings.ShelveInstances.Add(InstanceId, Settings.Settings.ShelveProperties);
                } else
                {
                    Settings.Settings.ShelveInstances.Add(InstanceId, new ObservableCollection<ShelveProperties>());
                }
            }

            Shelves = Settings.Settings.ShelveInstances[instanceId];

            foreach (var shelveProperties in Shelves)
            {
                ShelveViewModels.Add(new ShelveViewModel(shelveProperties, playniteAPI, ShelveViewModels));
            }

            AddShelveCommand = new RelayCommand(() =>
            {
                ShelveViewModel item = new ShelveViewModel(ShelveProperties.RecentlyPlayed, playniteAPI, ShelveViewModels);
                Shelves.Add(item.ShelveProperties);
                ShelveViewModels.Add(item);
                UpdateShelves();
            });

            RemoveShelveCommand = new RelayCommand<ShelveViewModel>(svm =>
            {
                if (svm != null)
                {
                    Shelves.Remove(svm.ShelveProperties);
                    ShelveViewModels.Remove(svm);
                    UpdateShelves();
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
                    Shelves.Insert(idx + 1, item.ShelveProperties);
                    ShelveViewModels.Insert(idx + 1, item);
                    UpdateShelves();
                }
            });
            MoveShelveUpCommand = new RelayCommand<ShelveViewModel>(svm =>
            {
                var idx = ShelveViewModels.IndexOf(svm);
                if (idx > 0)
                {
                    ShelveViewModels.Move(idx, idx - 1);
                    Shelves.Move(idx, idx - 1);
                }
                UpdateShelves();
            }, svm => ShelveViewModels.IndexOf(svm) > 0);

            MoveShelveDownCommand = new RelayCommand<ShelveViewModel>(svm =>
            {
                var idx = ShelveViewModels.IndexOf(svm);
                if (idx < ShelveViewModels.Count - 1)
                {
                    ShelveViewModels.Move(idx, idx + 1);
                    Shelves.Move(idx, idx + 1);
                }
                UpdateShelves();
            }, svm => ShelveViewModels.IndexOf(svm) < ShelveViewModels.Count - 1);
        }

        public Task UpdateShelves()
        {
            return Task.Run(() =>
            {
                foreach(var shelve in ShelveViewModels)
                {
                    shelve.UpdateGames(shelve.ShelveProperties);
                }
                GC.Collect();
                GC.Collect(1);
                GC.Collect(2);
            });
        }

        private void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
        {
            if (e.UpdatedItems.Any(u => IsRelevantUpdate(u)))
            {
                dispatcherTimer.Stop();
                if(e.UpdatedItems.Any(u => u.NewData.LastActivity != u.OldData.LastActivity))
                {
                    UpdateShelves();
                } else
                {
                    dispatcherTimer.Start();
                }
            }
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();
            dispatcherTimer.Tick -= DispatcherTimer_Tick;
            UpdateShelves();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
        }

        private void Games_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> e)
        {
            if (e.RemovedItems.Count + e.AddedItems.Count > 0)
            {
                dispatcherTimer.Stop();
                dispatcherTimer.Start();
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
            //if (old.BackgroundImage != updated.BackgroundImage) return true;
            //if (old.CoverImage != updated.CoverImage) return true;
            //if (old.Icon != updated.Icon) return true;
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

        public void UnsubscribeSettings()
        {
            Settings.PropertyChanged -= Settings_PropertyChanged1;
            Settings.Settings.PropertyChanged -= Settings_PropertyChanged;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LandingPageSettings.SkipGamesInPreviousShelves))
            {
                UpdateShelves();
            }
        }

        private void Settings_PropertyChanged1(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LandingPageSettingsViewModel.Settings))
            {
                if (Settings.Settings.ShelveInstances.ContainsKey(InstanceId))
                {
                    Settings.Settings.PropertyChanged += Settings_PropertyChanged;
                    Shelves = Settings.Settings.ShelveInstances[InstanceId];
                    for (int i = 0; i < Shelves.Count; ++i)
                    {
                        ShelveViewModels[i].ShelveProperties = Shelves[i];
                    }
                    UpdateShelves();
                } else
                {
                    Settings.PropertyChanged -= Settings_PropertyChanged1;
                }
            }
        }

        public void OnStartPageOpened()
        {
            UpdateShelves();
            Subscribe();
        }

        public void OnStartPageClosed()
        {
            Unsubscribe();
        }

        public void OnDayChanged(DateTime newTime)
        {
            UpdateShelves();
        }

        public void OnViewClosed()
        {
            Unsubscribe();
            UnsubscribeSettings();
            ShelveViewModels.ForEach(m => m.Games.Clear());
            ShelveViewModels.ForEach(m => m.OnViewClosed());
            ShelveViewModels.Clear();
        }
    }
}
