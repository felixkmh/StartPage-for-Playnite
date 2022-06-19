using LandingPage.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.ViewModels
{
    public class ShelvesSettingsViewModel : ObservableObject
    {
        LandingPageSettingsViewModel settings;
        public LandingPageSettings Settings { get => settings.Settings; }

        private ShelvesSettings shelvesSettings;
        public ShelvesSettings ShelvesSettings { get => shelvesSettings; set => SetValue(ref shelvesSettings, value); }

        public ShelvesSettingsViewModel(LandingPageSettingsViewModel settings, ShelvesSettings shelvesSettings)
        {
            this.settings = settings;
            ShelvesSettings = shelvesSettings;
        }
    }
}
