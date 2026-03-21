using LandingPage.Models;
using LandingPage.Models.SuccessStory;
using Playnite.SDK;
using StartPage.SDK.Async;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace LandingPage.ViewModels.PlayniteAchievements
{
    internal class PlayniteAchievementsViewModel : BusyObservableObject, IStartPageViewModel, IAsyncStartPageControl
    {
        internal ObservableCollection<IAchievement> latestAchievements = new ObservableCollection<IAchievement>();
        private readonly IPlayniteAPI _playniteApi;
        private readonly LandingPageSettings _settings;
        private readonly IAchievementsProvider _achievementsProvider;
        private object _lock = new object();
        public ICollectionView LatestAchievementsView { get; }

        public PlayniteAchievementsViewModel(IPlayniteAPI playniteApi, LandingPageSettings settings, IAchievementsProvider achievementsProvider)
        {
            BindingOperations.EnableCollectionSynchronization(latestAchievements, _lock);
            LatestAchievementsView = new ListCollectionView(latestAchievements);
            PropertyGroupDescription groupDescription = new PropertyGroupDescription(nameof(IAchievement.GameInfo));
            groupDescription.SortDescriptions.Add(new SortDescription("Name." + nameof(IGameAchievementsInfo.LastUnlock), ListSortDirection.Descending));
            LatestAchievementsView.GroupDescriptions.Add(groupDescription);

            LatestAchievementsView.SortDescriptions.Add(new SortDescription(nameof(IAchievement.UnlockedOn), ListSortDirection.Descending));
            _playniteApi = playniteApi;
            _settings = settings;
            _achievementsProvider = achievementsProvider;
        }

        public async Task InitializeAsync()
        {
            await Task.Yield();
            await ReloadAchievements();
            _achievementsProvider.OnAchievementsUpdates += OnAchievementsUpdates;
            _settings.PropertyChanged += _settings_PropertyChanged;
        }

        private void _settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LandingPageSettings.MaxNumberRecentAchievements) ||
                e.PropertyName == nameof(LandingPageSettings.MaxNumberRecentAchievementsPerGame))
            {
                _ = ReloadAchievements();
            }
        }

        private async Task ReloadAchievements()
        {
            try
            {
                var achievements = await _achievementsProvider.GetLatestAchievements(
                _settings.MaxNumberRecentAchievements,
                _settings.MaxNumberRecentAchievementsPerGame);

                lock (_lock)
                {
                    latestAchievements.Clear();
                    foreach (var achievement in achievements)
                    {
                        latestAchievements.Add(achievement);
                    }
                }

            }
            catch (Exception)
            {
                // Ignore
            }
        }

        private async Task OnAchievementsUpdates(CancellationToken token)
        {
            await ReloadAchievements();
        }

        public Task OnDayChangedAsync(DateTime newTime)
        {
            return Task.CompletedTask;
        }

        public void OnViewClosed()
        {
            
            _achievementsProvider.OnAchievementsUpdates -= OnAchievementsUpdates;
            _settings.PropertyChanged -= _settings_PropertyChanged;
            BindingOperations.DisableCollectionSynchronization(latestAchievements);
            if (_achievementsProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public Task OnViewHiddenAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnViewShownAsync()
        {
            return Task.CompletedTask;
        }
    }
}
