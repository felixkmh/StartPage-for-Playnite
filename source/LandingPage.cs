using LandingPage.ViewModels;
using LandingPage.Views;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;


namespace LandingPage
{
    public class LandingPage : GenericPlugin
    {
#pragma warning disable IDE0052 // Ungelesene private Member entfernen
        private static readonly ILogger logger = LogManager.GetLogger();
#pragma warning restore IDE0052 // Ungelesene private Member entfernen

        private LandingPageSettingsViewModel SettingsViewModel { get; set; }
        private LandingPageSettings Settings => SettingsViewModel.Settings;

        public override Guid Id { get; } = Guid.Parse("a6a3dcf6-9bfe-426c-afb0-9f49409ae0c5");


        internal LandingPageView view = null;
        internal LandingPageView View
        {
            get
            {
                if (view == null)
                {
                    view = new LandingPageView(ViewModel);
                }
                return view;
            }
        }

        internal LandingPageViewModel viewModel = null;
        internal LandingPageViewModel ViewModel
        {
            get
            {
                if (viewModel == null)
                {
                    ViewModels.SuccessStory.SuccessStoryViewModel successStory = null;
                    var path = Directory.GetDirectories(PlayniteApi.Paths.ExtensionsDataPath, "SuccessStory", SearchOption.AllDirectories).FirstOrDefault();
                    if (!string.IsNullOrEmpty(path))
                    {
                        successStory = new ViewModels.SuccessStory.SuccessStoryViewModel(path, PlayniteApi);
                        successStory.ParseAllAchievements();
                    }
                    viewModel = new LandingPageViewModel(PlayniteApi, this, SettingsViewModel, successStory);
                }
                viewModel.Update();
                return viewModel;
            }
        }

        public LandingPage(IPlayniteAPI api) : base(api)
        {
            SettingsViewModel = new LandingPageSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            var items = new List<SidebarItem>();

            items.Add(new SidebarItem {
                Type = SiderbarItemType.View,
                Title = "Start Page",
                Visible = true,
                Opened = () => { ViewModel.Subscribe(); return View; },
                Closed = () => { viewModel.Unsubscribe(); GC.Collect(); },
                Icon = new TextBlock { Text = "", FontFamily = ResourceProvider.GetResource<FontFamily>("FontIcoFont") }
            });

            return items;
        }

        

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            // Add code to be executed when game is started running.
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            var mainWindow = Application.Current.Windows.Cast<Window>().FirstOrDefault(w => w.Name == "WindowMain");
            if (mainWindow is Window)
            {
                if (Helper.UiHelper.FindVisualChildren<StackPanel>(mainWindow, "PART_PanelSideBarItems").FirstOrDefault() is StackPanel panel)
                {
                    if (Helper.UiHelper.FindVisualChildren(panel).FirstOrDefault(child => child.ToolTip?.ToString() == Settings.StartPage) is Button element)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action (() =>
                        {
                            if (element.Command.CanExecute(element))
                            {
                                element.Command.Execute(element);
                            }
                        }), System.Windows.Threading.DispatcherPriority.Normal);
                    }
                }
            }

            var path = Directory.GetDirectories(PlayniteApi.Paths.ExtensionsDataPath, "SuccessStory", SearchOption.AllDirectories).FirstOrDefault();
            if (!string.IsNullOrEmpty(path))
            {
                var successStory = new ViewModels.SuccessStory.SuccessStoryViewModel(path, PlayniteApi);
                successStory.ParseAllAchievements();
            }
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            // Add code to be executed when Playnite is shutting down.
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            // Add code to be executed when library is updated.
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return SettingsViewModel;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new LandingPageSettingsView();
        }
    }
}