using LandingPage.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.Settings
{
    public class ShelvesSettings : ObservableObject
    {
        private ObservableCollection<ShelveProperties> shelveProperties = new ObservableCollection<ShelveProperties>();
        public ObservableCollection<ShelveProperties> ShelveProperties { get => shelveProperties; set => SetValue(ref shelveProperties, value); }

        private bool horizontalLabels = false;
        public bool HorizontalLabels { get => horizontalLabels; set => SetValue(ref horizontalLabels, value); }

        private bool showDetails = true;
        public bool ShowDetails { get => showDetails; set => SetValue(ref showDetails, value); }

        private bool showTitleOnCover = true;
        public bool ShowTitleOnCover { get => showTitleOnCover; set => SetValue(ref showTitleOnCover, value); }

        private bool skipGamesInPreviousShelves = false;
        public bool SkipGamesInPreviousShelves { get => skipGamesInPreviousShelves; set => SetValue(ref skipGamesInPreviousShelves, value); }

        private double maxCoverWidth = 140;
        public double MaxCoverWidth { get => maxCoverWidth; set { SetValue(ref maxCoverWidth, value); } }

        private double trailerVolume = 0;
        public double TrailerVolume { get => trailerVolume; set { SetValue(ref trailerVolume, value); } }

        private bool showNotInstalledIndicator = false;
        public bool ShowNotInstalledIndicator { get => showNotInstalledIndicator; set => SetValue(ref showNotInstalledIndicator, value); }

        private bool showInstalledIndicator = false;
        public bool ShowInstalledIndicator { get => showInstalledIndicator; set => SetValue(ref showInstalledIndicator, value); }

        public ShelvesSettings()
        {

        }

        public ShelvesSettings(LandingPageSettings landingPageSettings)
        {
            showDetails = landingPageSettings.ShowDetails;
            showTitleOnCover = landingPageSettings.ShowTitleOnCover;
            skipGamesInPreviousShelves = landingPageSettings.SkipGamesInPreviousShelves;
            maxCoverWidth = landingPageSettings.MaxCoverWidth;
        }
    }
}
