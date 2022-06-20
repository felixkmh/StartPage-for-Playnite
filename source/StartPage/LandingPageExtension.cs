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
using System.Runtime.CompilerServices;
using LandingPage.Models;
using StartPage.SDK;
using LandingPage.Models.Layout;
using LandingPage.ViewModels.Layout;
using System.Diagnostics;
using LandingPage.ViewModels.GameActivity;
using Newtonsoft.Json;

namespace LandingPage
{
    public class LandingPageExtension : GenericPlugin, IStartPageExtension
    {
#pragma warning disable IDE0052 // Ungelesene private Member entfernen
        internal static readonly ILogger logger = LogManager.GetLogger();
#pragma warning restore IDE0052 // Ungelesene private Member entfernen

        public static LandingPageExtension Instance { get; private set; } = null;

        internal LandingPageSettingsViewModel SettingsViewModel { get; set; }
        internal LandingPageSettings Settings => SettingsViewModel.Settings;

        internal Dictionary<Guid, ShelvesViewModel> shelvesViewModels = new Dictionary<Guid, ShelvesViewModel>();

        public Dictionary<string, List<StartPageViewArgs>> AllAvailableViews { get; set; } = new Dictionary<string, List<StartPageViewArgs>>();

        public override Guid Id { get; } = Guid.Parse("a6a3dcf6-9bfe-426c-afb0-9f49409ae0c5");

        internal Lazy<GameActivityViewModel> gameActivityViewModel;

        internal LandingPageView view = null;
        internal LandingPageView View
        {
            get
            {
                if (view == null)
                {
                    view = new LandingPageView(ViewModel);
                    view.Resources.Add("SettingsModel", SettingsViewModel);
                }
                return view;
            }
        }

        public HashSet<Guid> RunningGames { get; } = new HashSet<Guid>();

        internal LandingPageViewModel viewModel = null;
        internal LandingPageViewModel ViewModel
        {
            get
            {
                if (viewModel == null)
                {
                    ViewModels.SuccessStory.SuccessStoryViewModel successStory = null;
                    ViewModels.GameActivity.GameActivityViewModel gameActivity = null;
                    var successStoryPath = Directory.GetDirectories(PlayniteApi.Paths.ExtensionsDataPath, "SuccessStory", SearchOption.AllDirectories).FirstOrDefault();
                    if (!string.IsNullOrEmpty(successStoryPath))
                    {
                        successStory = new ViewModels.SuccessStory.SuccessStoryViewModel(successStoryPath, PlayniteApi, SettingsViewModel);
                        successStory.ParseAllAchievements();
                    } else
                    {
                        successStory = new ViewModels.SuccessStory.SuccessStoryViewModel(null, PlayniteApi, SettingsViewModel);
                    }
                    var gameActivityPath = Directory.GetDirectories(PlayniteApi.Paths.ExtensionsDataPath, "GameActivity", SearchOption.AllDirectories).FirstOrDefault();
                    if (!string.IsNullOrEmpty(gameActivityPath))
                    {
                        gameActivity = new ViewModels.GameActivity.GameActivityViewModel(gameActivityPath, PlayniteApi, SettingsViewModel);
                        gameActivity.ParseAllActivites();
                    }
                    else
                    {
                        gameActivity = new ViewModels.GameActivity.GameActivityViewModel(null, PlayniteApi, SettingsViewModel);
                    }
                    viewModel = new LandingPageViewModel(PlayniteApi, this, SettingsViewModel, successStory, gameActivity);
                    foreach(var shelve in Settings.ShelveProperties)
                    {
                        //viewModel.ShelveViewModels.Add(new ShelveViewModel(shelve, PlayniteApi, viewModel));
                    }
                    viewModel.Update(true);
                } else
                {
                    viewModel.Update(false);
                }
                return viewModel;
            }
        }

        ProgressBar progressBar = null;
        TextBlock progressText = null;
        Button cancelButton = null;

        public LandingPageExtension(IPlayniteAPI api) : base(api)
        {
            Instance = this;
            SettingsViewModel = new LandingPageSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };

            gameActivityViewModel = new Lazy<GameActivityViewModel>(() =>
            {
                string gameActivityPath = null;
                string path = Path.Combine(PlayniteApi.Paths.ExtensionsDataPath, "afbb1a0d-04a1-4d0c-9afa-c6e42ca855b4", "GameActivity");
                if (Directory.Exists(path))
                {
                    gameActivityPath = path;
                }

                if (!string.IsNullOrEmpty(gameActivityPath))
                {
                    var model = new GameActivityViewModel(gameActivityPath, PlayniteApi, SettingsViewModel);
                    model.ParseAllActivites();
                    return model;
                }
                return null;
            });
        }

        internal StartPageView startPageView;
        internal StartPageViewModel startPageViewModel;
        private MostPlayedViewModel mostPlayedViewModel;

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            if (args.Games.Count == 1 && !string.IsNullOrEmpty(args.Games[0].BackgroundImage))
            {
                Action<GameMenuItemActionArgs> action = null;
                var path = args.Games[0].BackgroundImage;
                if (path.StartsWith("http", StringComparison.InvariantCultureIgnoreCase)
                    || File.Exists(path))
                {
                    if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var url))
                    {
                        action = a => { Settings.BackgroundImageUri = url; Settings.BackgroundImagePath = a.Games[0].Name; };
                    };
                } else if (PlayniteApi.Database.GetFullFilePath(path) is string databasePath)
                {
                    if (Uri.TryCreate(databasePath, UriKind.RelativeOrAbsolute, out var url))
                    {
                        action = a => { Settings.BackgroundImageUri = url; Settings.BackgroundImagePath = a.Games[0].Name; };
                    };
                }
                if (action != null)
                {
                    yield return new GameMenuItem
                    {
                        Action = action,
                        Description = ResourceProvider.GetString("LOC_SPG_SetGameAsBackground")
                    };
                }
            }     
        }


        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            var items = new List<SidebarItem>();

            //items.Add(new SidebarItem {
            //    Type = SiderbarItemType.View,
            //    Title = "Start Page",
            //    Visible = true,
            //    Opened = ViewOpened,
            //    Closed = ViewClosed,
            //    Icon = new TextBlock { Text = "", FontFamily = ResourceProvider.GetResource<FontFamily>("FontIcoFont") }
            //});

            items.Add(new SidebarItem
            {
                Type = SiderbarItemType.View,
                Title = "Start Page",
                Visible = true,
                Opened = ViewOpened,
                Closed = ViewClosed,
                Icon = new TextBlock { Text = "", FontFamily = ResourceProvider.GetResource<FontFamily>("FontIcoFont") }
            });

            return items;
        }

        private Control ViewOpened()
        {
            if (startPageView == null)
            {
                if (Settings.GridLayout == null)
                {
                    Settings.GridLayout = new GridNode { Orientation = Orientation.Vertical };
                    string path = Path.Combine(PlayniteApi.Paths.ConfigurationPath, "Extensions", "felixkmh_StartPage_Plugin", "DefaultLayout.json");
                    if (File.Exists(path))
                    {
                        if (JsonConvert.DeserializeObject<GridNode>(File.ReadAllText(path)) is GridNode node)
                        {
                            Settings.GridLayout = node;
                        }
                    }
                }

                startPageViewModel = new StartPageViewModel(PlayniteApi, this, SettingsViewModel, new GridNodeViewModel(Settings.GridLayout));

                var view = new StartPageView
                {
                    DataContext = startPageViewModel
                };

                if (Settings.EnableGlobalProgressBar && progressText is TextBlock && progressBar is ProgressBar && cancelButton is Button)
                {
                    view.ProgressbarGrid.SetBinding(FrameworkElement.VisibilityProperty, progressBar.GetBindingExpression(FrameworkElement.VisibilityProperty).ParentBinding);
                    view.ProgressText.SetBinding(TextBlock.TextProperty, progressText.GetBindingExpression(TextBlock.TextProperty).ParentBinding);
                    view.ProgressBar.SetBinding(ProgressBar.ValueProperty, progressBar.GetBindingExpression(ProgressBar.ValueProperty).ParentBinding);
                    view.ProgressBar.SetBinding(ProgressBar.MaximumProperty, progressBar.GetBindingExpression(ProgressBar.MaximumProperty).ParentBinding);
                    view.ProgressCancelButton.Command = cancelButton.Command;
                }

                startPageView = view;
            }
            if (startPageView.DataContext is StartPageViewModel model)
            {
                model.Opened();
            }

            return startPageView;
        }

        private void ViewClosed()
        {
            if (startPageView?.DataContext is StartPageViewModel model)
            {
                model.Closed();
            }
            GC.Collect();
        }

        public override void OnGameSelected(OnGameSelectedEventArgs args)
        {
            if (viewModel != null)
            {
                if (args.NewValue.FirstOrDefault() is Game last)
                {
                    viewModel.LastSelectedGame = last;
                }
            }
            if (startPageView?.DataContext is StartPageViewModel model)
            {
                if (args.NewValue.FirstOrDefault() is Game last)
                {
                    model.LastSelectedGame = last;
                }
            }
        }

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            RunningGames.Add(args.Game.Id);
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            RunningGames.Remove(args.Game.Id);
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            Application.Current.Deactivated += OnApplicattionDeactivated;
            // create tags
            if (Settings.EnableTagCreation)
            {
                if (Settings.IgnoreTagId == Guid.Empty || PlayniteApi.Database.Tags.Get(Settings.IgnoreTagId) == null)
                {
                    Settings.IgnoreTagId = PlayniteApi.Database.Tags.Add("[SPG] Ignored").Id;
                }
                if (Settings.IgnoreMostPlayedTagId == Guid.Empty || PlayniteApi.Database.Tags.Get(Settings.IgnoreMostPlayedTagId) == null)
                {
                    Settings.IgnoreMostPlayedTagId = PlayniteApi.Database.Tags.Add("[SPG] Most Played Ignored").Id;
                }
            }
            //Settings.IgnoreTagId = PlayniteApi.Database.Tags.Add("[SPG] Ignored").Id;
            //Settings.IgnoreMostPlayedTagId = PlayniteApi.Database.Tags.Add("[SPG] Most Played Ignored").Id;
            var switchWithLowPrio = false;
            var parentWindow = Application.Current.Windows.Cast<Window>().FirstOrDefault(w => w.Name == "WindowMain");
            parentWindow?.Dispatcher.Invoke(() => {
                if (Helper.UiHelper.FindVisualChildren<ProgressBar>(parentWindow, "PART_ProgressGlobal").FirstOrDefault() is ProgressBar bar)
                {
                    progressBar = bar;
                }
                if (Helper.UiHelper.FindVisualChildren<TextBlock>(parentWindow, "PART_TextProgressText").FirstOrDefault() is TextBlock info)
                {
                    progressText = info;
                }
                if (Helper.UiHelper.FindVisualChildren<Button>(parentWindow, "PART_ButtonProgressCancel").FirstOrDefault() is Button bt)
                {
                    cancelButton = bt;
                }
            });
            //Settings.SwitchWithLowPriority = true;
            //SavePluginSettings(Settings);
            if (Settings.EnableStartupOverride || Settings.MoveToTopOfList)
            {
                var mainWindow = Application.Current.Windows.Cast<Window>().FirstOrDefault(w => w.Name == "WindowMain");
                if (mainWindow is Window)
                {
                    mainWindow.Dispatcher.Invoke(() => 
                    {
                        if (Helper.UiHelper.FindVisualChildren<StackPanel>(mainWindow, "PART_PanelSideBarItems").FirstOrDefault() is StackPanel panel)
                        {
                            if (Helper.UiHelper.FindVisualChildren(panel).FirstOrDefault(child => child.ToolTip?.ToString() == Settings.StartPage) is Button element)
                            {
                                var childIndex = -1;
                                if (Settings.MoveToTopOfList)
                                {
                                    childIndex = panel.Children.Cast<FrameworkElement>().ToList().FindIndex(c => c.ToolTip?.ToString() == "Start Page");
                                }

                                panel.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    if (childIndex > -1)
                                    {
                                        var child = panel.Children[childIndex];
                                        panel.Children.RemoveAt(childIndex);
                                        panel.Children.Insert(0, child);
                                    }

                                    if (Settings.EnableStartupOverride)
                                    {
                                        if (element.Command.CanExecute(element))
                                        {
                                            element.Command.Execute(element);
                                        }
                                    }
                                }), switchWithLowPrio ? System.Windows.Threading.DispatcherPriority.ApplicationIdle : System.Windows.Threading.DispatcherPriority.DataBind);

                            }
                        }
                    }, switchWithLowPrio ? System.Windows.Threading.DispatcherPriority.ApplicationIdle : System.Windows.Threading.DispatcherPriority.DataBind);
                }
            }
            foreach(var plugin in PlayniteApi.Addons.Plugins)
            {
                if (plugin is IStartPageExtension extension)
                {
                    if (extension.GetAvailableStartPageViews() is StartPageExtensionArgs extensionArgs 
                        && extensionArgs.Views != null
                        && extensionArgs.Views.Any())
                    {
                        AllAvailableViews.Add(extensionArgs.ExtensionName, extensionArgs.Views.Select(v => new StartPageViewArgs(v, plugin.Id)).ToList());
                    }
                }
            }
        }

        private void OnApplicattionDeactivated(object sender, EventArgs e)
        {
            if (startPageViewModel != null)
            {
                if (startPageViewModel.RootNodeViewModel.EditModeEnabled)
                {
                    startPageViewModel.ExitEditModeCommand.Execute(null);
                }
            }
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            // Add code to be executed when Playnite is shutting down.
            if (viewModel is LandingPageViewModel)
            {
                // Settings.ShelveProperties = viewModel.ShelveViewModels.Select(svm => svm.ShelveProperties).ToList();
            }
            if (startPageView?.DataContext is StartPageViewModel startPageViewModel)
            {
                Settings.GridLayout = startPageViewModel.RootNodeViewModel.GridNode;
                Settings.ShelveInstanceSettings = shelvesViewModels.ToDictionary(p => p.Key, p => p.Value.Shelves);
            }


            // collapse layout
            if (Settings.GridLayout != null)
            {
                GridNode.Minimize(Settings.GridLayout, null);
            }
            SavePluginSettings(Settings);
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

        public StartPageExtensionArgs GetAvailableStartPageViews()
        {
            var views = new List<StartPageViewArgsBase>();
            var args = new StartPageExtensionArgs() { ExtensionName = "StartPage", Views = views };

            string successStoryPath = null;
            string path = Path.Combine(PlayniteApi.Paths.ExtensionsDataPath, "cebe6d32-8c46-4459-b993-5a5189d60788", "SuccessStory");
            if (Directory.Exists(path))
            {
                successStoryPath = path;
            }
            if (!string.IsNullOrEmpty(successStoryPath))
            {
                views.Add(new StartPageViewArgsBase 
                { 
                    ViewId = "RecentAchivements", 
                    Name = ResourceProvider.GetString("LOC_SPG_RecentAchievementsView"), 
                    Description = ResourceProvider.GetString("LOC_SPG_RecentAchievementsDescription")
                });
            }

            string gameActivityPath = null;
            string path2 = Path.Combine(PlayniteApi.Paths.ExtensionsDataPath, "afbb1a0d-04a1-4d0c-9afa-c6e42ca855b4", "GameActivity");
            if (Directory.Exists(path2))
            {
                gameActivityPath = path2;
            }
            if (!string.IsNullOrEmpty(gameActivityPath))
            {
                views.Add(new StartPageViewArgsBase 
                { 
                    ViewId = "WeeklyActivity", 
                    Name = ResourceProvider.GetString("LOC_SPG_WeeklyActivityView"), 
                    Description = ResourceProvider.GetString("LOC_SPG_WeeklyActivityDescription")
                });
            }

            views.Add(new StartPageViewArgsBase 
            { 
                ViewId = "GameShelves", 
                Name = ResourceProvider.GetString("LOC_SPG_ShelvesView"), 
                Description = ResourceProvider.GetString("LOC_SPG_ShelvesViewDescription"),
                HasSettings = true,
                AllowMultipleInstances = true
            });
            views.Add(new StartPageViewArgsBase 
            { 
                ViewId = "MostPlayed", 
                Name = ResourceProvider.GetString("LOC_SPG_MostPlayedView"), 
                Description = ResourceProvider.GetString("LOC_SPG_MostPlayedDescription"),
                HasSettings = true
            });
            views.Add(new StartPageViewArgsBase 
            { 
                ViewId = "DigitalClock", 
                Name = ResourceProvider.GetString("LOC_SPG_ClockView"), 
                Description = ResourceProvider.GetString("LOC_SPG_ClockViewDescription"),
            });
            return args;
        }

        private Task gameActivityTask = Task.CompletedTask;

        public object GetStartPageView(string id, Guid instanceId)
        {
            if (id == "RecentAchivements")
            {
                string successStoryPath = null;
                string path = Path.Combine(PlayniteApi.Paths.ExtensionsDataPath, "cebe6d32-8c46-4459-b993-5a5189d60788", "SuccessStory");
                if (Directory.Exists(path))
                {
                    successStoryPath = path;
                }
                if (!string.IsNullOrEmpty(successStoryPath))
                {
                    var successStoryViewModel = new ViewModels.SuccessStory.SuccessStoryViewModel(successStoryPath, PlayniteApi, SettingsViewModel);
                    Task.Run(() =>
                    {
                        successStoryViewModel.ParseAllAchievements();
                    }).ContinueWith(t =>
                    {
                        t?.Dispose();
                        successStoryViewModel.Update();
                    });
                    var view = new RecentAchievementsView() 
                    { 
                        DataContext = successStoryViewModel
                    };
                    return view;
                }
            }
            if (id == "GameShelves")
            {
                var viewModel = new ShelvesViewModel(PlayniteApi, this, SettingsViewModel, instanceId);
                shelvesViewModels.Add(instanceId, viewModel);
                return new ShelvesView { DataContext = viewModel };
            }
            if (id == "MostPlayed")
            {
                string gameActivityPath = null;
                string path = Path.Combine(PlayniteApi.Paths.ExtensionsDataPath, "afbb1a0d-04a1-4d0c-9afa-c6e42ca855b4", "GameActivity");
                if (Directory.Exists(path))
                {
                    gameActivityPath = path;
                }

                if (!string.IsNullOrEmpty(gameActivityPath))
                {
                    mostPlayedViewModel = new MostPlayedViewModel(PlayniteApi, SettingsViewModel, gameActivityViewModel.Value);
                    var view = new MostPlayedView() { DataContext = mostPlayedViewModel };

                    return view;
                }
            }
            if (id == "DigitalClock")
            {
                var model = new { Clock = new Clock() };
                return new ClockView() { DataContext = model };
            }
            if (id == "WeeklyActivity")
            {
                string gameActivityPath = null;
                string path = Path.Combine(PlayniteApi.Paths.ExtensionsDataPath, "afbb1a0d-04a1-4d0c-9afa-c6e42ca855b4", "GameActivity");
                if (Directory.Exists(path))
                {
                    gameActivityPath = path;
                }
                if (!string.IsNullOrEmpty(gameActivityPath))
                {
                    var view = new GameActivityView() { DataContext = gameActivityViewModel.Value };
                    return view;
                }
            }
            return null;
        }

        public Control GetStartPageViewSettings(string id, Guid instanceId)
        {
            if (id == "MostPlayed")
            {
                return new Views.Settings.MostPlayedSettingsView() { DataContext = SettingsViewModel };
            }
            if (id == "GameShelves")
            {
                return new Views.Settings.ShelvesSettingsView() { DataContext = new ViewModels.ShelvesSettingsViewModel(SettingsViewModel, Settings.ShelveInstanceSettings[instanceId]) };
            }
            return null;
        }

        public void OnViewRemoved(string viewId, Guid instanceId)
        {
            if (viewId == "GameShelves")
            {
                shelvesViewModels[instanceId].OnViewClosed();
                shelvesViewModels.Remove(instanceId);
                Settings.ShelveInstanceSettings.Remove(instanceId);
            }
            if (viewId == "MostPlayed")
            {
                mostPlayedViewModel?.OnViewClosed();
                mostPlayedViewModel = null;
            }
            if (viewId == "WeeklyActivity")
            {

            }
            GC.Collect();
            GC.Collect(1);
            GC.Collect(2);
        }
    }
}