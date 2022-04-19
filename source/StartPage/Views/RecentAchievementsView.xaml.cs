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
using static LandingPage.ViewModels.SuccessStory.SuccessStoryViewModel;

namespace LandingPage.Views
{
    /// <summary>
    /// Interaktionslogik für RecentAchievementsView.xaml
    /// </summary>
    public partial class RecentAchievementsView : UserControl
    {
        public RecentAchievementsView()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button bt)
            {
                if (bt.DataContext is CollectionViewGroup group)
                {
                    if (group.Items.FirstOrDefault() is GameAchievement a)
                    {
                        a.Game?.OpenCommand?.Execute(null);
                    }
                }
            }
        }
    }
}
