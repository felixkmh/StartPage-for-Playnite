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

namespace LandingPage
{
    public class LandingPageExtension : GenericPlugin
    {
#pragma warning disable IDE0052 // Ungelesene private Member entfernen
        internal static readonly ILogger logger = LogManager.GetLogger();
#pragma warning restore IDE0052 // Ungelesene private Member entfernen

        public static LandingPageExtension Instance { get; private set; } = null;

        internal LandingPageSettingsViewModel SettingsViewModel { get; set; }
        internal LandingPageSettings Settings => SettingsViewModel.Settings;

        public override Guid Id { get; } = Guid.Parse("a6a3dcf6-9bfe-426c-afb0-9f49409ae0c5");


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
                        successStory = new ViewModels.SuccessStory.SuccessStoryViewModel(path, PlayniteApi, SettingsViewModel);
                        successStory.ParseAllAchievements();
                    } else
                    {
                        successStory = new ViewModels.SuccessStory.SuccessStoryViewModel(null, PlayniteApi, SettingsViewModel);
                    }
                    viewModel = new LandingPageViewModel(PlayniteApi, this, SettingsViewModel, successStory);
                    viewModel.Update(true);
                } else
                {
                    viewModel.Update(false);
                }
                return viewModel;
            }
        }

        public LandingPageExtension(IPlayniteAPI api) : base(api)
        {
            Instance = this;
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
                Opened = ViewOpened,
                Closed = ViewClosed,
                Icon = new TextBlock { Text = "", FontFamily = ResourceProvider.GetResource<FontFamily>("FontIcoFont") }
            });

            return items;
        }

        private Control ViewOpened()
        {
            ViewModel.Subscribe();
            return View;
        }

        private void ViewClosed()
        {
            viewModel?.Unsubscribe();
            if (!Settings.KeepInMemory)
            {
                view.DataContext = null;
                view = null;
                viewModel.successStory = null;
                viewModel.clock = null;
                viewModel = null;
                GC.Collect();
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
            var switchWithLowPrio = false;
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
#if DEBUG
            //Task.Run(() =>
            //{
            //    var rng = new Random();
            //    for (var i = 0; i < 10; ++i)
            //    {
            //        int n = i + 1;
            //        Thread.Sleep(rng.Next(10000));
            //        NotificationType type = NotificationType.Info;
            //        if (rng.NextDouble() < 0.5)
            //        {
            //            type = NotificationType.Error;
            //        }
            //        if (rng.NextDouble() < 0.5)
            //        {
            //            PlayniteApi.Notifications.Add(new NotificationMessage($"{i}_test", $"Longer Notification #{n} with activation action that activates on click!!",
            //                type,
            //                () => PlayniteApi.Dialogs.ShowMessage($"You clicked on Notifaction #{n}")));
            //        }
            //        else
            //        {
            //            PlayniteApi.Notifications.Add(new NotificationMessage($"{i}_test", $"Notification #{n}!!", type));
            //        }
            //    }
            //});
#endif
            //Settings.SwitchWithLowPriority = switchWithLowPrio;
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            // Add code to be executed when Playnite is shutting down.
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
    }
}