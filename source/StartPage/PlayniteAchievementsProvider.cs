using LandingPage.Models;
using LandingPage.Models.SuccessStory;
using Playnite.SDK;
using Playnite.SDK.Data;
using SqlNado;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace LandingPage
{
    internal class PlayniteAchievementsProvider : IAchievementsProvider, IDisposable
    {
        private string _libraryPath;
        private readonly IPlayniteAPI _playniteAPI;
        private FileSystemWatcher _fileWatcher;
        private System.Timers.Timer _timer;

        public PlayniteAchievementsProvider(IPlayniteAPI playniteAPI)
        {
            _libraryPath = FindAchievementCachePath(playniteAPI.Paths.ExtensionsDataPath)
                ?? Path.Combine(playniteAPI.Paths.ExtensionsDataPath, "e6aad2c9-6e06-4d8d-ac55-ac3b252b5f7b", "achievement_cache.db");

            _playniteAPI = playniteAPI;
            var watchDir = Path.GetDirectoryName(_libraryPath);
            if (!string.IsNullOrWhiteSpace(watchDir) && Directory.Exists(watchDir))
            {
                _fileWatcher = new FileSystemWatcher(watchDir, "achievement_cache.db");
                _fileWatcher.Changed += _fileWatcher_Changed;
                _fileWatcher.EnableRaisingEvents = true;
            }
            _timer = new System.Timers.Timer(TimeSpan.FromSeconds(4).TotalMilliseconds) { AutoReset = false };
            _timer.Elapsed += _timer_Elapsed;
        }

        private async void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var timer = (System.Timers.Timer)sender;

            timer.Stop();
            try
            {
                await OnAchievementsUpdates?.Invoke(default);
            }
            catch (Exception ex)
            {
                LandingPageExtension.logger.Error(ex, 
                    "Error while invoking handlers for OnAchievementsUpdates in PlayniteAchievementsProvider");
            }
        }

        private void _fileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            _timer.Stop();
            _timer.Start();
        }

        public event AchievementsUpdatedCallback OnAchievementsUpdates;

        class AchievementDto : INotifyPropertyChanged, IAchievement
        {
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public string UnlockedIconPath { get; set; }
            public string LockedIconPath { get; set; }
            public string PlayniteGameId { get; set; }
            public DateTime UnlockTimeUtc { get; set; }
            public int AchievementsUnlocked { get; set; }
            public int TotalAchievements { get; set; }

            public string Name => DisplayName;

            public bool IsUnlocked => true;

            public IGameAchievementsInfo GameInfo { get; set; }

            public Uri Icon { get; set; }

            public DateTime? UnlockedOn => UnlockTimeUtc;

            public event PropertyChangedEventHandler PropertyChanged { add { } remove { } }
        }

        const string GAMES_WITH_LATEST_ACHIEVEMENT =
@"
SELECT 
                     MAX(COALESCE(ua.UnlockTimeUtc, ugp.LastUpdatedUtc)) as LastUnlock,
                     COALESCE(g.PlayniteGameId, ugp.CacheKey) AS GameId,
       MAX(ugp.AchievementsUnlocked) AS AchievementsUnlocked,
       MAX(ugp.TotalAchievements) AS TotalAchievements
FROM UserAchievements AS ua
JOIN UserGameProgress ugp
    ON ugp.Id = ua.UserGameProgressId
JOIN AchievementDefinitions ad
    ON ad.Id = ua.AchievementDefinitionId 
JOIN Games g 
    ON g.Id = ad.GameId
   AND g.Id = ugp.GameId
WHERE ua.Unlocked = 1
            AND COALESCE(g.PlayniteGameId, ugp.CacheKey) IS NOT NULL
        GROUP BY COALESCE(g.PlayniteGameId, ugp.CacheKey)
ORDER BY LastUnlock DESC
LIMIT @limit OFFSET @offset
";
        private const string LATEST_ACHIEVEMENTS =
@"SELECT
    ad.DisplayName, ad.Description, ad.UnlockedIconPath, ad.LockedIconPath,
    COALESCE(ua.UnlockTimeUtc, ugp.LastUpdatedUtc) AS UnlockTimeUtc,
    COALESCE(g.PlayniteGameId, ugp.CacheKey) AS PlayniteGameId,
    ugp.AchievementsUnlocked,
    ugp.TotalAchievements
FROM UserAchievements ua
JOIN UserGameProgress ugp
    ON ugp.Id = ua.UserGameProgressId
JOIN AchievementDefinitions ad
    ON ad.Id = ua.AchievementDefinitionId 
JOIN Games g 
    ON g.Id = ad.GameId
   AND g.Id = ugp.GameId
WHERE ua.Unlocked = 1
ORDER BY COALESCE(ua.UnlockTimeUtc, ugp.LastUpdatedUtc) DESC
LIMIT @limit OFFSET @offset
";

        private const string LATEST_ACHIEVEMENTS_FOR_GAME_ID =
@"
SELECT
    ad.DisplayName, ad.Description, ad.UnlockedIconPath, ad.LockedIconPath,
    COALESCE(ua.UnlockTimeUtc, ugp.LastUpdatedUtc) AS UnlockTimeUtc,
    COALESCE(g.PlayniteGameId, ugp.CacheKey) AS PlayniteGameId
FROM UserAchievements ua
JOIN UserGameProgress ugp
    ON ugp.Id = ua.UserGameProgressId
JOIN AchievementDefinitions ad
    ON ad.Id = ua.AchievementDefinitionId 
JOIN Games g 
    ON g.Id = ad.GameId
   AND g.Id = ugp.GameId
WHERE ua.Unlocked = 1
    AND COALESCE(g.PlayniteGameId, ugp.CacheKey) = @gameId
ORDER BY COALESCE(ua.UnlockTimeUtc, ugp.LastUpdatedUtc) DESC
LIMIT @limit OFFSET @offset
";

        public async Task<List<IAchievement>> GetLatestAchievements(
            int maxAchievemnets,
            int maxAchievementsPerGame,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            List<IAchievement> results = new List<IAchievement>();
            var iconBasePath = Path.GetDirectoryName(_libraryPath) ?? _playniteAPI.Paths.ExtensionsDataPath;
            using (var db = new SQLiteDatabase(_libraryPath, SQLiteOpenOptions.SQLITE_OPEN_READONLY))
            {
                try
                {
                    var latestAchievements = db.Load<AchievementDto>(
                        LATEST_ACHIEVEMENTS,
                        maxAchievemnets * Math.Max(1, maxAchievementsPerGame),
                        0);

                    var perGameCounts = new Dictionary<Guid, int>();
                    var gameInfoCache = new Dictionary<Guid, GameInfo>();

                    foreach (var achievement in latestAchievements)
                    {
                        if (results.Count >= maxAchievemnets)
                        {
                            break;
                        }

                        if (!Guid.TryParse(achievement.PlayniteGameId, out var playniteGameId))
                        {
                            continue;
                        }

                        if (maxAchievementsPerGame > 0 &&
                            perGameCounts.TryGetValue(playniteGameId, out var currentPerGameCount) &&
                            currentPerGameCount >= maxAchievementsPerGame)
                        {
                            continue;
                        }

                        var gameModel = _playniteAPI.Database.Games.Get(playniteGameId);
                        if (gameModel == null)
                        {
                            continue;
                        }

                        if (!gameInfoCache.TryGetValue(playniteGameId, out var gameInfo))
                        {
                            gameInfo = new GameInfo
                            {
                                GameId = playniteGameId,
                                GameName = gameModel.Name,
                                AchievementsUnlocked = achievement.AchievementsUnlocked,
                                TotalAchievements = achievement.TotalAchievements,
                                LastUnlock = achievement.UnlockTimeUtc
                            };
                            gameInfoCache[playniteGameId] = gameInfo;
                        }

                        achievement.GameInfo = gameInfo;
                        if (achievement.UnlockedIconPath is string path && !string.IsNullOrEmpty(path))
                        {
                            if (path.StartsWith("https://") || path.StartsWith("http://"))
                            {
                                achievement.Icon = new Uri(path);
                            }
                            else if (Path.IsPathRooted(path) && File.Exists(path))
                            {
                                achievement.Icon = new Uri(path);
                            }
                            else
                            {
                                var combinedPath = Path.Combine(iconBasePath, path);
                                if (File.Exists(combinedPath))
                                {
                                    achievement.Icon = new Uri(combinedPath);
                                }
                            }
                        }

                        results.Add(achievement);
                        perGameCounts[playniteGameId] = perGameCounts.TryGetValue(playniteGameId, out var count)
                            ? count + 1
                            : 1;

                    }
                }
                catch (Exception ex)
                {
                    _ = ex;
                }
            }
            return results;
        }

        public void Dispose()
        {
            _fileWatcher.Changed -= _fileWatcher_Changed;
            ((IDisposable)_fileWatcher)?.Dispose();
            _timer?.Dispose();
        }

        internal static string FindAchievementCachePath(string extensionsDataPath)
        {
            if (string.IsNullOrWhiteSpace(extensionsDataPath) || !Directory.Exists(extensionsDataPath))
            {
                return null;
            }

            try
            {
                var candidates = Directory.EnumerateFiles(extensionsDataPath, "achievement_cache.db", SearchOption.AllDirectories)
                    .ToList();

                if (candidates.Count == 0)
                {
                    return null;
                }

                var ranked = candidates
                    .Select(path => new
                    {
                        Path = path,
                        Score = ScoreAchievementCache(path),
                        LastWriteUtc = File.GetLastWriteTimeUtc(path)
                    })
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.LastWriteUtc)
                    .FirstOrDefault();

                return ranked?.Path;
            }
            catch
            {
                return null;
            }
        }

        private static long ScoreAchievementCache(string dbPath)
        {
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
            {
                return long.MinValue;
            }

            long score = 0;
            try
            {
                using (var db = new SQLiteDatabase(dbPath, SQLiteOpenOptions.SQLITE_OPEN_READONLY))
                {
                    var unlockedCount = db.ExecuteScalar<long>(
                        "SELECT COUNT(1) FROM UserAchievements WHERE Unlocked = 1;");
                    if (unlockedCount > 0)
                    {
                        score += 10_000_000_000L;
                    }

                    var localUnlockedCount = db.ExecuteScalar<long>(
                        @"SELECT COUNT(1)
                          FROM UserAchievements ua
                          JOIN AchievementDefinitions ad ON ad.Id = ua.AchievementDefinitionId
                          JOIN Games g ON g.Id = ad.GameId
                          WHERE ua.Unlocked = 1
                            AND g.ProviderKey = 'Local';");
                    if (localUnlockedCount > 0)
                    {
                        score += 1_000_000_000L;
                    }

                    var maxUnlock = db.ExecuteScalar<string>(
                        "SELECT MAX(UnlockTimeUtc) FROM UserAchievements WHERE Unlocked = 1;");
                    if (DateTime.TryParse(maxUnlock, out var parsedUnlock))
                    {
                        score += parsedUnlock.ToUniversalTime().Ticks;
                    }
                }
            }
            catch
            {
                // Fall back to filesystem timestamp scoring if SQL probing fails.
            }

            try
            {
                score += File.GetLastWriteTimeUtc(dbPath).Ticks;
            }
            catch
            {
            }

            return score;
        }
    }

    internal class GameInfo : INotifyPropertyChanged, IGameAchievementsInfo
    {
        public DateTime Latest { get; set; }
        public Guid GameId { get; set; }
        public int AchievementsUnlocked { get; set; }
        public int TotalAchievements { get; set; }

        public int UnlockedAchievements => AchievementsUnlocked;

        public double Progress => TotalAchievements > 0 ? (double)AchievementsUnlocked / TotalAchievements : 0;

        public string GameName { get; set; }

        public DateTime? LastUnlock { get; set; }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }
}
