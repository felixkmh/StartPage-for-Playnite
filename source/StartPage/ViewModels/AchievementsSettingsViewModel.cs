using LandingPage.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.ViewModels
{
    internal class AchievementsSettingsViewModel : ObservableObject
    {
        private readonly LandingPageSettings settings;
        public AchievementsSettingsViewModel(LandingPageSettings settings)
        {
            this.settings = settings;
        }

        public AchievementsProvider CurrentProvider
        {
            get => settings.SelectedAchievementsProvider;
            set { 
                settings.SelectedAchievementsProvider = value;
                OnPropertyChanged();
            }
        }
    }
}
