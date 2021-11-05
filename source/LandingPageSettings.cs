using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LandingPage
{
    public class LandingPageSettings : ObservableObject
    {
        // workaround to get this module to be loaded by Playnite
        private Gu.Wpf.NumericInput.DoubleBox _ = new Gu.Wpf.NumericInput.DoubleBox();

        private string startPage = ResourceProvider.GetString("LOCLibrary");
        public string StartPage { get => startPage; set => SetValue(ref startPage, value); }

        private bool showClock = true;
        public bool ShowClock { get => showClock; set => SetValue(ref showClock, value); }

        private bool showRecentGames = true;
        public bool ShowRecentGames { get => showRecentGames; set => SetValue(ref showRecentGames, value); }

        private bool showAddedGames = true;
        public bool ShowAddedGames { get => showAddedGames; set => SetValue(ref showAddedGames, value); }

        private bool showTitleOnCover = true;
        public bool ShowTitleOnCover { get => showTitleOnCover; set => SetValue(ref showTitleOnCover, value); }

        private int maxNumberRecentAchievements = 6;
        public int MaxNumberRecentAchievements { get => maxNumberRecentAchievements; set => SetValue(ref maxNumberRecentAchievements, value); }

        private int maxNumberRecentAchievementsPerGame = 3;
        public int MaxNumberRecentAchievementsPerGame { get => maxNumberRecentAchievementsPerGame; set => SetValue(ref maxNumberRecentAchievementsPerGame, value); }

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
            }
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