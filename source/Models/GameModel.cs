using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        }

        public Game Game { get; }

        public ICommand OpenCommand { get; set; }

        public ICommand StartCommand { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
