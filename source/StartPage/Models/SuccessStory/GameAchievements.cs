using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.Models.SuccessStory
{
    public class GameAchievement : ObservableObject, IAchievement
    {
        internal GameModel game;
        public GameModel Game { get => game; set { if (value != game) { game = value; OnPropertyChanged(); } } }
        internal Achievement achievement;
        public Achievement Achievement { get => achievement; set { if (value != achievement) { achievement = value; OnPropertyChanged(); } } }
        internal Achievements source;
        public Achievements Source { get => source; set { if (value != source) { source = value; OnPropertyChanged(); } } }

        public string Name => achievement.Name;

        public string Description => achievement.Description;

        public bool IsUnlocked => achievement.DateUnlocked != null;

        IGameAchievementsInfo IAchievement.GameInfo => Source;

        public Uri Icon => achievement.UriUnlocked;

        public DateTime? UnlockedOn => achievement.DateUnlocked;
    }
}
