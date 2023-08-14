using LandingPage.Models;
using LandingPage.ViewModels;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using static LandingPage.ViewModels.ShelveViewModel;
using PlayniteCommon.UI;

namespace LandingPage.Views
{
    /// <summary>
    /// Interaktionslogik für LandingPageView.xaml
    /// </summary>
    public partial class LandingPageView : UserControl
    {
        public LandingPageView()
        {
            InitializeComponent();
        }

        public LandingPageView(LandingPageViewModel model)
        {
            DataContext = model;
            InitializeComponent();
        }

        private void ListBoxItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item)
            {
                if (item.DataContext is GameModel model)
                {
                    model.OpenCommand?.Execute(null);
                }
            }
        }

        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item)
            {
                if (item.DataContext is GameModel model)
                {
                    model.StartCommand?.Execute(null);
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

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged || e.WidthChanged)
            {
                _ = Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var newWidth = ActualWidth;
                        var listBoxes = ShelvesItemsControl.ItemContainerGenerator.Items
                        .Select(item => ShelvesItemsControl.ItemContainerGenerator.ContainerFromItem(item))
                        .OfType<ContentPresenter>()
                        .Select(ele => ele.ContentTemplate.FindName("GamesListBox", ele))
                        .OfType<ListBox>();
                        foreach (var listBox in sender is ListBox lb ? new[] { lb } : listBoxes)
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
                                var availableWidth = newWidth - 120;
                                var newListWidth = Math.Floor(availableWidth / Math.Max(itemWidth, 1)) * itemWidth;
                                if (listBox.MaxWidth != newListWidth)
                                {
                                    listBox.MaxWidth = newListWidth;
                                }
                            }
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void ToggleNotifications_Click(object sender, RoutedEventArgs e)
        {
            if (NotificationsBorder.Tag is Visibility vis)
            {
                if (vis == Visibility.Visible)
                {
                    NotificationsBorder.Tag = Visibility.Collapsed;
                } else
                {
                    NotificationsBorder.SetBinding(TagProperty, new Binding("Notifications") { Converter = (IValueConverter)Resources["IEnumerableNullOrEmptyToVisibilityConverter"], Mode = BindingMode.OneWay });
                }
            }
        }

        static readonly Random rng = new Random();

        private void Description_Closed(object sender, EventArgs e)
        {
            if (rng.NextDouble() <= 0.25)
            {
                GC.Collect();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button bt)
            {
                if (bt.DataContext is ShelveViewModel svm)
                {
                    if (svm.ShelveProperties.Order == Order.Ascending)
                    {
                        svm.ShelveProperties.Order = Order.Descending;
                    } else
                    {
                        svm.ShelveProperties.Order = Order.Ascending;
                    }
                }
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

        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {

        }

        private void ComboBox_DropDownClosed(object sender, EventArgs e)
        {

        }

        private void Description_Opened(object sender, EventArgs e)
        {

        }
    }
}
