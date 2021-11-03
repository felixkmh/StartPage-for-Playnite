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

        public struct GameAchievement
        {
            public GameModel Game { get; set; }
            public Achivement Achievement { get; set; }
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
            var latest = achievements.Values
                .SelectMany(v => v.Items)
                .Where(a => (!a.DateUnlocked?.Equals(default(DateTime))) ?? false)
                .OrderByDescending(a => a.DateUnlocked ?? default)
                .Take(10)
                .Select(a => new GameAchievement { Game = null, Achievement = a});
            latestAchievements.Update(latest);
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
