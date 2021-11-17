using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LandingPage.Models
{
    public class GameModel : ObservableObject
    {
        public GameModel(Game game)
        {
            Game = game;
            OpenCommand = new RelayCommand(() =>
            {
                try
                {
                    if (game is Game && LandingPageExtension.Instance.PlayniteApi.Database.Games.Get(Game.Id) is Game)
                    {
                        LandingPageExtension.Instance.PlayniteApi.MainView.SwitchToLibraryView();
                        LandingPageExtension.Instance.PlayniteApi.MainView.SelectGame(Game.Id);
                    }
                }
                catch (Exception ex)
                {
                    LandingPageExtension.logger.Error(ex, $"Error when switching to library view to select game: {Game.Name} (ID: {Game.Id})");
                }
            });
            StartCommand = new RelayCommand(() =>
            {
                if (game is Game && LandingPageExtension.Instance.PlayniteApi.Database.Games.Get(Game.Id) is Game)
                {
                    LandingPageExtension.Instance.PlayniteApi.StartGame(Game.Id);
                }
            });
        }

        private Game game = null;
        public Game Game { get => game; set { if (value != game) { SetValue(ref game, value); OnPropertyChanged(nameof(LogoUri)); } } }

        public Uri LogoUri
        {
            get
            {
                var metadataPath = Path.Combine(LandingPageExtension.Instance.PlayniteApi.Paths.ConfigurationPath, "ExtraMetadata", "games", game.Id.ToString());
                if (Directory.Exists(metadataPath))
                {
                    var logoPath = Path.Combine(metadataPath, "Logo.png");
                    if (File.Exists(logoPath) && Uri.TryCreate(logoPath, UriKind.RelativeOrAbsolute, out var uri))
                    {
                        return uri;
                    }
                }
                return null;
            }
        }

        public ICommand OpenCommand { get; private set; }

        public ICommand StartCommand { get; private set; }
    }
}
