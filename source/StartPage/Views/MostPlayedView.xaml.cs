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

        private void Description_Closed(object sender, EventArgs e)
        {
            if (rng.NextDouble() <= 0.25)
            {
                GC.Collect();
            }
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
