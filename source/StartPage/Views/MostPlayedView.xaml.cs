using LandingPage.Models;
using LandingPage.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PlayniteCommon.UI;
using System.Windows.Threading;

namespace LandingPage.Views
{
    /// <summary>
    /// Interaktionslogik für MostPlayedView.xaml
    /// </summary>
    public partial class MostPlayedView : UserControl
    {
        public MostPlayedView()
        {
            InitializeComponent();
        }

        static readonly Random rng = new Random();

        public void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateWidth(sender, e);
        }

        private void UpdateWidth(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged || e.WidthChanged)
            {
                _ = Dispatcher.BeginInvoke(new Action(() =>
                {
                    var newWidth = ActualWidth;
                    var listBoxes = GameGroups.ItemContainerGenerator.Items;
                    var items = listBoxes.Select(item => GameGroups.ItemContainerGenerator.ContainerFromItem(item));
                    var presenters = items.OfType<ListBoxItem>();
                    var lists = presenters.Select(ele => UiHelper.FindVisualChildren<ListBox>(ele, "GroupList").FirstOrDefault());
                    foreach (var listBox in lists)
                    {
                        var itemCount = listBox.ItemsSource?.Cast<object>().Count() ?? 0;
                        if (listBox.IsVisible && itemCount > 0)
                        {
                            FrameworkElement container = null;
                            for (int i = 0; i < itemCount; ++i)
                            {
                                if (listBox.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement element)
                                {
                                    container = element;
                                    break;
                                }
                            }
                            var desiredWidth = listBox.DesiredSize.Width;
                            var itemWidth = container.ActualWidth + container.Margin.Left + container.Margin.Right;
                            var scrollViewer = UiHelper.FindVisualChildren<ScrollViewer>(listBox).FirstOrDefault();
                            // itemWidth = desiredWidth / itemCount;
                            var availableWidth = newWidth / lists.Count();
                            FrameworkElement panel = VisualTreeHelper.GetParent(this) as FrameworkElement;
                            while (!(panel is GridNodeView))
                            {
                                panel = VisualTreeHelper.GetParent(panel) as FrameworkElement;
                            }
                            availableWidth = (panel.ActualWidth - 14 * lists.Count()) / lists.Count();
                            var newListWidth = availableWidth;

                            presenters.ForEach(p => p.MaxWidth = panel.ActualWidth / lists.Count());

                            foreach (var gameItem in listBox.Items.OfType<object>().Select(item => listBox.ItemContainerGenerator.ContainerFromItem(item)).OfType<FrameworkElement>())
                            {
                                double newHeight = newListWidth / LandingPageExtension.Instance.Settings.CoverAspectRatio;
                                if (newHeight != gameItem.MaxHeight)
                                {
                                    gameItem.MaxHeight = newHeight;
                                }
                            }
                        }
                        
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void Description_Closed(object sender, EventArgs e)
        {
            if (rng.NextDouble() <= 0.25)
            {
                GC.Collect();
            }
        }

        private static DispatcherTimer dispatcherTimer = null;

        private static GameModel clickedModel = null;

        private void ListBoxItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && !(e.OriginalSource is TextBlock))
            {
                if (dispatcherTimer == null)
                {
                    dispatcherTimer = new DispatcherTimer();
                    dispatcherTimer.Interval = TimeSpan.FromMilliseconds(250);
                    dispatcherTimer.Tick += DispatcherTimer_Tick;
                }
                dispatcherTimer.Start();

                if (item.DataContext is GameModel model)
                {
                    clickedModel = model;
                    //model.OpenCommand?.Execute(null);
                }
            }
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();
            clickedModel?.OpenCommand?.Execute(null);
        }

        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item)
            {
                if (item.DataContext is GameModel model)
                {
                    dispatcherTimer?.Stop();
                    model.StartCommand?.Execute(null);
                    e.Handled = true;
                }
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button bt && bt.DataContext is GameModel game)
            {
                game.StartCommand?.Execute(null);
            }
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button bt && bt.DataContext is GameModel game)
            {
                game.OpenCommand?.Execute(null);
            }
        }

        private void StackPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is GameModel model)
            {
                if (DataContext is LandingPageViewModel viewModel)
                {
                    viewModel.CurrentlyHoveredGame = model.Game;
                }
            }
        }

        private void StackPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is GameModel model)
            {
                if (DataContext is LandingPageViewModel viewModel)
                {
                    viewModel.CurrentlyHoveredGame = null;
                }
            }
        }
    }
}
