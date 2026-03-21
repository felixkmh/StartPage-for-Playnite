using LandingPage.ViewModels.PlayniteAchievements;
using Playnite.SDK;
using StartPage.SDK.Async;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.ViewModels
{
    internal class RecentAchievementsViewModel : BusyObservableObject, IStartPageViewModel, IAsyncStartPageControl
    {
        private IAsyncStartPageControl _inner;
        private readonly LandingPageSettingsViewModel _settings;

        public RecentAchievementsViewModel(LandingPageSettingsViewModel settings)
        {
            _settings = settings;
            _inner = GetInnerViewModel();

            _settings.Settings.PropertyChanged += Settings_PropertyChanged;
        }

        private IAsyncStartPageControl GetInnerViewModel()
        {
            if (_settings.Settings.SelectedAchievementsProvider is Settings.AchievementsProvider.SuccessStory)
            {
                string successStoryPath = null;
                string path = Path.Combine(API.Instance.Paths.ExtensionsDataPath, "cebe6d32-8c46-4459-b993-5a5189d60788", "SuccessStory");
                if (Directory.Exists(path))
                {
                    successStoryPath = path;
                }
                if (!string.IsNullOrEmpty(successStoryPath))
                {
                    return new SuccessStory.SuccessStoryViewModel(successStoryPath, API.Instance, _settings);
                }
            }
            if (_settings.Settings.SelectedAchievementsProvider is Settings.AchievementsProvider.PlayniteAchievements)
            {
                string path = Path.Combine(API.Instance.Paths.ExtensionsDataPath, "e6aad2c9-6e06-4d8d-ac55-ac3b252b5f7b", "achievement_cache.db");
                if (File.Exists(path))
                {
                    return new PlayniteAchievementsViewModel(API.Instance, _settings.Settings, new PlayniteAchievementsProvider(API.Instance));
                }
            }
            return null;
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(LandingPageSettings.SelectedAchievementsProvider))
            {
                Inner = GetInnerViewModel();
            }
        }

        public IAsyncStartPageControl Inner { get => _inner; set
            {
                if (_inner is IStartPageViewModel startPageViewModel)
                {
                    startPageViewModel.OnViewClosed();
                }
                if (_inner is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                SetValue(ref _inner, value);
                _ = _inner?.InitializeAsync().ContinueWith(t => _inner?.OnViewShownAsync());
            } }

        public Task InitializeAsync()
        {
            return _inner?.InitializeAsync() ?? Task.CompletedTask;
        }

        public Task OnDayChangedAsync(DateTime newTime)
        {
            return _inner?.OnDayChangedAsync(newTime) ?? Task.CompletedTask;
        }

        public void OnViewClosed()
        {
            _settings.Settings.PropertyChanged -= Settings_PropertyChanged;
            if (_inner is IStartPageViewModel spvm)
            { 
                spvm.OnViewClosed();
            }
            if (_inner is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public Task OnViewHiddenAsync()
        {
            return _inner?.OnViewHiddenAsync() ?? Task.CompletedTask;
        }

        public Task OnViewShownAsync()
        {
            return _inner?.OnViewShownAsync() ?? Task.CompletedTask;
        }
    }
}
