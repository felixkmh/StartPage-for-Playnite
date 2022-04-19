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

namespace LandingPage.Controls
{
    /// <summary>
    /// Interaktionslogik für TrailerPlayer.xaml
    /// </summary>
    public partial class TrailerPlayer : MediaElement
    {
        public TrailerPlayer()
        {
            InitializeComponent();
        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Source = null;
        }
    }
}
