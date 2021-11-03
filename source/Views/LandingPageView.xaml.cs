using LandingPage.Models;
using LandingPage.ViewModels;
using Playnite.SDK.Models;
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
    }
}
