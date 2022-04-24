using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.Models
{
    public class GameGroup : ObservableObject
    {
        private string label = "Group";
        public string Label { get => label; set => SetValue(ref label, value); }

        public ObservableCollection<GameModel> Games { get; } = new ObservableCollection<GameModel>();
    }
}
