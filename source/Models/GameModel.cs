using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LandingPage.Models
{
    public class GameModel : INotifyPropertyChanged
    {
        public GameModel(Game game)
        {
            Game = game;
            OpenCommand = new RelayCommand(() =>
            {
                LandingPageExtension.Instance.PlayniteApi.MainView.SwitchToLibraryView();
                LandingPageExtension.Instance.PlayniteApi.MainView.SelectGame(Game.Id);
            });
            StartCommand = new RelayCommand(() =>
            {
                LandingPageExtension.Instance.PlayniteApi.StartGame(Game.Id);
            });
        }

        private Game game = null;
        public Game Game { get => game; set { if (value != game) { game = value; OnPropertyChanged(); } } }

        public ICommand OpenCommand { get; private set; }

        public ICommand StartCommand { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
