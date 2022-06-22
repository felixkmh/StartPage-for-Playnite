using LandingPage.Models;
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
    /// Interaktionslogik für GameDetailsView.xaml
    /// </summary>
    public partial class GameDetailsView : UserControl
    {
        public GameDetailsView()
        {
            InitializeComponent();
            //DataContextChanged += GameDetailsView_DataContextChanged;
        }

        //private void GameDetailsView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        //{
        //    if (e.NewValue is GameModel model)
        //    {
        //        var playniteApi = LandingPageExtension.Instance.PlayniteApi;
        //        var dir = System.IO.Path.Combine(playniteApi.Paths.ConfigurationPath, "ExtraMetadata", "games", model.Game.Id.ToString());
        //        var trailerPath = System.IO.Path.Combine(dir, "VideoTrailer.mp4");
        //        if (System.IO.File.Exists(trailerPath))
        //        {
        //            VideoPlayer.Source = new Uri(trailerPath);
                    
        //            return;
        //        }
        //        var microTrailerPath = System.IO.Path.Combine(dir, "VideoMicrotrailer.mp4");
        //        if (System.IO.File.Exists(microTrailerPath))
        //        {
        //            VideoPlayer.Source = new Uri(microTrailerPath);
                    
        //            return;
        //        }
        //    } 
        //    VideoPlayer.Source = null;
        //}
    }
}
