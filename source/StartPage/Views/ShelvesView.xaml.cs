using LandingPage.Models;
using LandingPage.ViewModels;
using PlayniteCommon.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LandingPage.Views
{
    /// <summary>
    /// Interaktionslogik für ShelvesView.xaml
    /// </summary>
    public partial class ShelvesView : UserControl
    {
        public ShelvesView()
        {
            InitializeComponent();
        }

        public bool ShowDetails
        {
            get { return (bool)GetValue(ShowDetailsProperty); }
            set 
            { 
                SetValue(ShowDetailsProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for ShowDetails.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowDetailsProperty =
            DependencyProperty.Register(nameof(ShowDetails), typeof(bool), typeof(ShelvesView), new PropertyMetadata(false));



        private static DispatcherTimer dispatcherTimer = null;

        private static GameModel clickedModel = null;

        private void ListBoxItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && !(e.OriginalSource is TextBlock))
            {
                if (dispatcherTimer == null)
                {
                    dispatcherTimer = new DispatcherTimer(DispatcherPriority.Background, Application.Current.Dispatcher);
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
                Dispatcher.Invoke(() => {
                    ShowDetails = false;
                }, System.Windows.Threading.DispatcherPriority.Normal);

                game.StartCommand?.Execute(null);
            }
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button bt && bt.DataContext is GameModel game)
            {
                Dispatcher.Invoke(() => {
                    ShowDetails = false;
                }, System.Windows.Threading.DispatcherPriority.Normal);
                game.OpenCommand?.Execute(null);
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
                    }
                    else
                    {
                        svm.ShelveProperties.Order = Order.Ascending;
                    }
                }
            }
        }

        //private void StackPanel_MouseEnter(object sender, MouseEventArgs e)
        //{
        //    if (sender is FrameworkElement element && element.DataContext is GameModel model && model.Game != ShelveViewModel.DummyGame)
        //    {
        //        if (DataContext is ShelvesViewModel shelvesViewModel && shelvesViewModel.Shelves.ShowDetails)
        //        {
        //            if (UiHelper.FindVisualChildren<Grid>(element, "ImageGrid").FirstOrDefault() is Grid imageGrid)
        //            {
        //                infoPopup.Dispatcher.Invoke(() => {
        //                    infoPopup.DataContext = element.DataContext;
        //                    infoPopup.Description.PlacementTarget = imageGrid;
        //                    infoPopup.Description.IsOpen = true;
        //                    if (infoPopup.Player != null)
        //                    {
        //                        infoPopup.Player.IsMuted = shelvesViewModel.Shelves.TrailerVolume <= 0.0;
        //                        infoPopup.Player.Volume = shelvesViewModel.Shelves.TrailerVolume;
        //                    }
        //                }, System.Windows.Threading.DispatcherPriority.Normal);
        //            }
        //        }

        //        if (DataContext is ShelvesViewModel viewModel)
        //        {
        //            viewModel.CurrentlyHoveredGame = model.Game;
        //        }
        //    }
        //}

        //private void StackPanel_MouseLeave(object sender, MouseEventArgs e)
        //{
        //    if (sender is FrameworkElement element && element.DataContext is GameModel model)
        //    {
        //        if (UiHelper.FindVisualChildren<Grid>(element, "ImageGrid").FirstOrDefault() is Grid imageGrid)
        //        {
        //            infoPopup.Dispatcher.Invoke(() => {
        //                if (infoPopup.Player != null)
        //                {
        //                    infoPopup.Player.Volume = 0;
        //                }
        //                infoPopup.Description.IsOpen = false;
        //            }, System.Windows.Threading.DispatcherPriority.Normal);
        //        }

        //        if (DataContext is ShelvesViewModel viewModel)
        //        {
        //            viewModel.CurrentlyHoveredGame = null;
        //        }
        //    }
        //}

        private void Description_Opened(object sender, EventArgs e)
        {
            foreach (var window in Application.Current.Windows.Cast<Window>())
            {
                var type = window.GetType();
            }
        }
    }
}
