using LandingPage.Models;
using Playnite.SDK.Controls;
using Playnite.SDK.Plugins;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace LandingPage.Views
{
    /// <summary>
    /// Interaktionslogik für GameDetailsPopup.xaml
    /// </summary>
    public partial class GameDetailsPopup : Popup
    {   
        public double Volume
        {
            get { return (double)GetValue(VolumeProperty); }
            set { SetValue(VolumeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Volume.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VolumeProperty =
            DependencyProperty.Register("Volume", typeof(double), typeof(GameDetailsPopup), new PropertyMetadata(0.0));



        public MediaElement Player { get; private set; }

        public GameDetailsPopup()
        {
            InitializeComponent();
            DataContextChanged += GameDetailsPopup_DataContextChanged;
            var loader = LandingPageExtension.Instance.PlayniteApi.Addons.Plugins.FirstOrDefault(p => p.Id == Guid.Parse("705fdbca-e1fc-4004-b839-1d040b8b4429"));
            if (loader is GenericPlugin plugin)
            {
                var player = plugin.GetGameViewControl(new GetGameViewControlArgs { Mode = Playnite.SDK.ApplicationMode.Desktop, Name = "VideoLoaderControl_NoControls_Sound" });
                VideoLoaderControl_NoControls_Sound.Content = player;
                Player = player.FindName("player") as MediaElement;
            }
        }

        private void GameDetailsPopup_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (VideoLoaderControl_NoControls_Sound.Content is PluginUserControl control)
            {
                if (DataContext is GameModel model)
                {
                    control.GameContext = model.Game;
                } else
                {
                    control.GameContext = null;
                }

            }
        }

        static readonly Random rng = new Random();

        private void Description_Closed(object sender, EventArgs e)
        {
            if (Player is MediaElement element)
            {
                element.Volume = 0;
                element.IsMuted = element.Volume <= 0;
            }
            if (rng.NextDouble() <= 0.25)
            {
                GC.Collect();
            }
        }

        private void Popup_Opened(object sender, EventArgs e)
        {
            if (Player is MediaElement element)
            {
                element.Volume = Math.Pow(Volume, 2);
                element.IsMuted = element.Volume <= 0; 
            }
        }
    }
}
