using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using LandingPage.Models.SuccessStory;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.ObjectModel;
using LandingPage.Models;
using LandingPage.Extensions;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LandingPage.ViewModels.SuccessStory
{
    public class SuccessStoryViewModel
    {
        internal string achievementsPath;
        internal IPlayniteAPI playniteAPI;
        internal Dictionary<Guid, Achievements> achievements = new Dictionary<Guid, Achievements>();
        public Dictionary<Guid, Achievements> Achievements => achievements;

        internal ObservableCollection<GameAchievement> latestAchievements = new ObservableCollection<GameAchievement>();
        public ObservableCollection<GameAchievement> LatestAchievements
        {
            get
            {
                return latestAchievements;
            }
        }

        public class GameAchievement : INotifyPropertyChanged
        {
            public GameModel game;
            public GameModel Game { get => game; set { if (value != game) { game = value; OnPropertyChanged(); } } }
            public Achivement achievement;
            public Achivement Achievement { get => achievement; set { if (value != achievement) { achievement = value; OnPropertyChanged(); } } }
            public Achievements source;
            public Achievements Source { get => source; set { if (value != source) { source = value; OnPropertyChanged(); } } }


            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged([CallerMemberName] string propertyName = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public SuccessStoryViewModel(string achievementsPath, IPlayniteAPI playniteAPI)
        {
            this.achievementsPath = achievementsPath;
            this.playniteAPI = playniteAPI;
        }

        public void Update()
        {
            UpdateLatestAchievements();
        }

        public void UpdateLatestAchievements()
        {
            var latest = achievements
                .SelectMany(pair => pair.Value.Items
                    .OrderByDescending(a => a.DateUnlocked ?? default)
                    .Take(2)
                    .Select(a => new { Game = playniteAPI.Database.Games.Get(pair.Value.Id), Achievement = a, Source = pair.Value }))
                .Where(a => (!a.Achievement.DateUnlocked?.Equals(default(DateTime))) ?? false)
                .OrderByDescending(a => a.Achievement.DateUnlocked ?? default)
                .Take(6);
            Application.Current.Dispatcher.Invoke(() => 
            {
                int i = 0;
                foreach (var achievement in latest)
                {
                    if (latestAchievements.Count > i)
                    {
                        latestAchievements[i].Game.Game = achievement.Game;
                        latestAchievements[i].Achievement = achievement.Achievement;
                        latestAchievements[i].Source = achievement.Source;
                    }
                    else
                    {
                        latestAchievements.Add(new GameAchievement
                        {
                            Game = new GameModel(achievement.Game),
                            Achievement = achievement.Achievement,
                            Source = achievement.Source
                        });
                    }
                    ++i;
                }
                for (int j = latestAchievements.Count - 1; j >= i; --j)
                {
                    latestAchievements.RemoveAt(j);
                }
            });
        }

        public void ParseAllAchievements()
        {
            if (Directory.Exists(achievementsPath))
            {
                achievements = Directory.GetFiles(achievementsPath)
                    .AsParallel()
                    .Where(path => Guid.TryParse(Path.GetFileNameWithoutExtension(path), out var id) && playniteAPI.Database.Games.Get(id) is Game)
                    .Select(path => DeserializeAchievementsFile(path))
                    .OfType<Achievements>()
                    .Where(ac => ac.HaveAchivements)
                    .ToDictionary(ac => ac.Id);
            }
        }

        public bool ParseAchievements(Guid gameId)
        {
            if (playniteAPI.Database.Games.Get(gameId) is Game)
            {
                var path = Directory.GetFiles(achievementsPath, gameId.ToString().ToLower() + ".json").FirstOrDefault();
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        var gameAchievements = DeserializeAchievementsFile(path);
                        if (gameAchievements.HaveAchivements)
                        {
                            achievements[gameId] = gameAchievements;
                            return true;
                        }
                    }
                    catch (Exception) {}

                }
            }
            return false;
        }

        internal Achievements DeserializeAchievementsFile(string path)
        {
            try
            {
                using (var fileStream = File.OpenRead(path))
                using (var textReader = new StreamReader(fileStream))
                using (var reader = new JsonTextReader(textReader))
                {
                    var serializer = new JsonSerializer();
                    return serializer.Deserialize<Achievements>(reader);
                }
            }
            catch (Exception) {}
            return null;
        }
    }
}
