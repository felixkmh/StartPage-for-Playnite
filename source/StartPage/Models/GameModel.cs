using LandingPage.Converters;
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
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace LandingPage.Models
{
    public class GameModel : ObservableObject
    {
        private readonly static GameSource DefaultGameSource = new GameSource("Playnite");

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
                    Application.Current.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        (Action<Guid>)LandingPageExtension.Instance.PlayniteApi.StartGame,
                        Game.Id);
                }
            });
        }

        private Game game = null;
        public Game Game
        { 
            get => game;
            set
            {
                if (value != game) 
                {
                    SetValue(ref game, value);
                    OnPropertyChanged(nameof(CoverImagePath));
                    OnPropertyChanged(nameof(LogoUri));
                    OnPropertyChanged(nameof(SortingName));
                    OnPropertyChanged(nameof(TrailerUri));
                }
            }
        }

        public string CoverImagePath => LandingPageExtension.Instance.PlayniteApi.Database.GetFullFilePath(Game.CoverImage);

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

        public Uri TrailerUri 
        { 
            get 
            {
                var playniteApi = LandingPageExtension.Instance.PlayniteApi;
                var dir = System.IO.Path.Combine(playniteApi.Paths.ConfigurationPath, "ExtraMetadata", "games", Game.Id.ToString());
                var trailerPath = System.IO.Path.Combine(dir, "VideoTrailer.mp4");
                if (System.IO.File.Exists(trailerPath))
                {
                    return new Uri(trailerPath);
                }
                var microTrailerPath = System.IO.Path.Combine(dir, "VideoMicrotrailer.mp4");
                if (System.IO.File.Exists(microTrailerPath))
                {
                    return new Uri(microTrailerPath);
                }
                return null;
            } 
        }

        public string SortingName => string.IsNullOrEmpty(Game?.SortingName) ? Game?.Name : Game?.SortingName;
        public GameSource Source => Game?.Source ?? DefaultGameSource;

        public ICommand OpenCommand { get; private set; }

        public ICommand StartCommand { get; private set; }
    }
}
