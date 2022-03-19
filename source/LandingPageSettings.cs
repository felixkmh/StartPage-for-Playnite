using LandingPage.Converters;
using LandingPage.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LandingPage
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum BackgroundImageSource
    {
        [Description("LOC_SPG_BackgroundImageSourceLastPlayed")]
        LastPlayed,
        [Description("LOC_SPG_BackgroundImageSourceLastAdded")]
        LastAdded,
        [Description("LOC_SPG_BackgroundImageSourceMostPlayed")]
        MostPlayed,
        [Description("LOC_SPG_BackgroundImageSourceRandom")]
        Random,
        [Description("LOC_SPG_BackgroundImageSourceLastSelected")]
        LastSelected,
        [Description("LOC_SPG_BackgroundImageSourceLastHovered")]
        LastHovered
    }

    [Serializable]
    public class LayoutProperties : ObservableObject
    {
        internal GridLength upperRowHeight = new GridLength(1.0, GridUnitType.Star);
        public GridLength UpperRowHeight { get => upperRowHeight; set => SetValue(ref upperRowHeight, value); }

        internal GridLength lowerRowHeight = new GridLength(1.2, GridUnitType.Star);
        public GridLength LowerRowHeight { get => lowerRowHeight; set => SetValue(ref lowerRowHeight, value); }
    }

    public class LandingPageSettings : ObservableObject
    {
        // workaround to get this module to be loaded by Playnite
        private Gu.Wpf.NumericInput.DoubleBox _ = new Gu.Wpf.NumericInput.DoubleBox();

        private string startPage = ResourceProvider.GetString("LOCLibrary");
        public string StartPage { get => startPage; set => SetValue(ref startPage, value); }

        private bool showClock = true;
        public bool ShowClock { get => showClock; set => SetValue(ref showClock, value); }

        private bool showDetails = true;
        public bool ShowDetails { get => showDetails; set => SetValue(ref showDetails, value); }

        private bool showRecentAchievements = true;
        public bool ShowRecentAchievements { get => showRecentAchievements; set => SetValue(ref showRecentAchievements, value); }

        private bool showRecentGames = true;
        public bool ShowRecentGames { get => showRecentGames; set => SetValue(ref showRecentGames, value); }

        private bool showMostPlayedGames = true;
        public bool ShowMostPlayedGames { get => showMostPlayedGames; set => SetValue(ref showMostPlayedGames, value); }

        private bool showAddedGames = true;
        public bool ShowAddedGames { get => showAddedGames; set => SetValue(ref showAddedGames, value); }

        private bool showFavoriteGames = false;
        public bool ShowFavoriteGames { get => showFavoriteGames; set => SetValue(ref showFavoriteGames, value); }

        private bool showTitleOnCover = true;
        public bool ShowTitleOnCover { get => showTitleOnCover; set => SetValue(ref showTitleOnCover, value); }

        private int maxNumberRecentAchievements = 6;
        public int MaxNumberRecentAchievements { get => maxNumberRecentAchievements; set => SetValue(ref maxNumberRecentAchievements, value); }

        private int maxNumberRecentAchievementsPerGame = 3;
        public int MaxNumberRecentAchievementsPerGame { get => maxNumberRecentAchievementsPerGame; set => SetValue(ref maxNumberRecentAchievementsPerGame, value); }

        private bool enableNotifications = true;
        public bool EnableNotifications { get => enableNotifications; set => SetValue(ref enableNotifications, value); }

        private bool enableGlobalProgressBar = false;
        public bool EnableGlobalProgressBar { get => enableGlobalProgressBar; set => SetValue(ref enableGlobalProgressBar, value); }

        private bool enableGameActivity = true;
        public bool EnableGameActivity { get => enableGameActivity; set => SetValue(ref enableGameActivity, value); }

        private bool minimizeNotificationsOnLaunch = false;
        public bool MinimizeNotificationsOnLaunch { get => minimizeNotificationsOnLaunch; set => SetValue(ref minimizeNotificationsOnLaunch, value); }

        private bool showNotificationButtons = true;
        public bool ShowNotificationButtons { get => showNotificationButtons; set => SetValue(ref showNotificationButtons, value); }

        private bool enableStartupOverride = false;
        public bool EnableStartupOverride { get => enableStartupOverride; set => SetValue(ref enableStartupOverride, value); }

        public bool switchWithLowPriority = false;
        public bool SwitchWithLowPriority { get => switchWithLowPriority; set => SetValue(ref switchWithLowPriority, value); }

        private bool keepInMemory = true;
        public bool KeepInMemory { get => keepInMemory; set => SetValue(ref keepInMemory, value); }

        private double blurAmount = 20;
        public double BlurAmount { get => blurAmount; set { SetValue(ref blurAmount, value); OnPropertyChanged(nameof(BlurAmountScaled)); } }

        private double coverAspectRatio = 0.71794;
        public double CoverAspectRatio { get => coverAspectRatio; set { SetValue(ref coverAspectRatio, value); } }

        private double maxCoverWidth = 140;
        public double MaxCoverWidth { get => maxCoverWidth; set { SetValue(ref maxCoverWidth, value); } }

        private int numberOfGames = 10;
        public int NumberOfGames { get => numberOfGames; set { SetValue(ref numberOfGames, value); } }

        [DontSerialize]
        public double BlurAmountScaled { get => Math.Round(blurAmount / renderScale); }

        private double animationDuration = 1;
        public double AnimationDuration { get => animationDuration; set => SetValue(ref animationDuration, value); }

        private int backgroundRefreshInterval = 0;
        public int BackgroundRefreshInterval { get => backgroundRefreshInterval; set => SetValue(ref backgroundRefreshInterval, value); }

        private double backgroundGameInfoOpacity = 0.11;
        public double BackgroundGameInfoOpacity { get => backgroundGameInfoOpacity; set => SetValue(ref backgroundGameInfoOpacity, value); }

        private double overlayOpacity = 0.1;
        public double OverlayOpacity { get => overlayOpacity; set => SetValue(ref overlayOpacity, value); }

        private double noiseOpacity = 0.15;
        public double NoiseOpacity { get => noiseOpacity; set => SetValue(ref noiseOpacity, value); }

        private double renderScale = 0.05;
        public double RenderScale { get => renderScale; set { SetValue(ref renderScale, value); OnPropertyChanged(nameof(BlurAmountScaled)); } }

        private LayoutProperties layoutSettings = new LayoutProperties();
        public LayoutProperties LayoutSettings { get => layoutSettings; set => SetValue(ref layoutSettings, value); }

        private bool moveToTopOfList = false;
        public bool MoveToTopOfList { get => moveToTopOfList; set => SetValue(ref moveToTopOfList, value); }

        private string backgroundImagePath = null;
        public string BackgroundImagePath { get => backgroundImagePath; set => SetValue(ref backgroundImagePath, value); }

        private Uri backgroundImageUri = null;
        public Uri BackgroundImageUri { get => backgroundImageUri; set => SetValue(ref backgroundImageUri, value); }

        private BackgroundImageSource backgroundImageSource = BackgroundImageSource.LastPlayed;
        public BackgroundImageSource BackgroundImageSource { get => backgroundImageSource; set => SetValue(ref backgroundImageSource, value); }

        private List<ShelveProperties> shelveProperties = null;
        public List<ShelveProperties> ShelveProperties { get => shelveProperties; set => SetValue(ref shelveProperties, value); }

        private bool skipGamesInPreviousShelves = false;
        public bool SkipGamesInPreviousShelves { get => skipGamesInPreviousShelves; set => SetValue(ref skipGamesInPreviousShelves, value); }

        private Guid ignoreTagId;
        public Guid IgnoreTagId
        {
            get
            {
                if (LandingPageExtension.Instance.PlayniteApi != null && ignoreTagId == Guid.Empty)
                {
                    ignoreTagId = LandingPageExtension.Instance.PlayniteApi.Database.Tags.Add("[SPG] Ignored").Id;
                }
                else if (LandingPageExtension.Instance?.PlayniteApi != null && LandingPageExtension.Instance.PlayniteApi.Database.Tags.Get(ignoreTagId) == null)
                {
                    ignoreTagId = LandingPageExtension.Instance.PlayniteApi.Database.Tags.Add("[SPG] Ignored").Id;
                }
                return ignoreTagId;
            }
            set => ignoreTagId = value;
        }

        private Guid ignoreMostPlayedTagId;
        public Guid IgnoreMostPlayedTagId
        {
            get
            {
                if (LandingPageExtension.Instance.PlayniteApi != null && ignoreMostPlayedTagId == Guid.Empty)
                {
                    ignoreMostPlayedTagId = LandingPageExtension.Instance.PlayniteApi.Database.Tags.Add("[SPG] Most Played Ignored").Id;
                }
                else if (LandingPageExtension.Instance?.PlayniteApi != null && LandingPageExtension.Instance.PlayniteApi.Database.Tags.Get(ignoreMostPlayedTagId) == null)
                {
                    ignoreMostPlayedTagId = LandingPageExtension.Instance.PlayniteApi.Database.Tags.Add("[SPG] Most Played Ignored").Id;
                }
                return ignoreMostPlayedTagId;
            }
            set => ignoreMostPlayedTagId = value;
        }

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        [DontSerialize]
        public IEnumerable<string> StartPageOptions
        {
            get
            {
                var mainWindow = Application.Current.Windows.Cast<Window>().FirstOrDefault(w => w.Name == "WindowMain");
                if (mainWindow is Window)
                {
                    if (Helper.UiHelper.FindVisualChildren<StackPanel>(mainWindow, "PART_PanelSideBarItems").FirstOrDefault() is StackPanel panel)
                    {
                        return panel.Children.Cast<FrameworkElement>().Select(e => e.ToolTip?.ToString()).OfType<string>();
                    }
                }
                return new List<string>();
            }
        }
    }

    public class LandingPageSettingsViewModel : ObservableObject, ISettings
    {
        private readonly LandingPageExtension plugin;
        private LandingPageSettings editingClone { get; set; }

        private LandingPageSettings settings;
        public LandingPageSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public ICommand SelectImagePathCommand { get; private set; }
        public ICommand SelectImageFolderPathCommand { get; private set; }
        public ICommand ClearImagePathCommand { get; private set; }

        public LandingPageSettingsViewModel(LandingPageExtension plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<LandingPageSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new LandingPageSettings();
                Settings.ShelveProperties = new List<ShelveProperties> { ShelveProperties.RecentlyPlayed, ShelveProperties.RecentlyAdded };
            }

            if (Settings.ShelveProperties == null)
            {
                Settings.ShelveProperties = new List<ShelveProperties> { ShelveProperties.RecentlyPlayed, ShelveProperties.RecentlyAdded };
            }

            SelectImagePathCommand = new RelayCommand(() =>
            {
                if (plugin.PlayniteApi.Dialogs.SelectImagefile() is string path)
                {
                    if (System.IO.File.Exists(path))
                    {
                        if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var uri))
                        {
                            Settings.BackgroundImageUri = uri;
                            Settings.BackgroundImagePath = path;
                        }
                    }
                }
            });

            SelectImageFolderPathCommand = new RelayCommand(() =>
            {
                if (plugin.PlayniteApi.Dialogs.SelectFolder() is string path)
                {
                    if (System.IO.Directory.Exists(path))
                    {
                        Settings.BackgroundImagePath = path;
                        Settings.BackgroundImageUri = null;
                    }
                }
            });

            ClearImagePathCommand = new RelayCommand(() =>
            {
                Settings.BackgroundImageUri = null;
                Settings.BackgroundImagePath = null;
            });
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = editingClone;
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }
    }
}