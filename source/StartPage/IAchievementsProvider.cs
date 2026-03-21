using LandingPage.Models;
using LandingPage.Models.SuccessStory;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LandingPage
{
    delegate Task AchievementsUpdatedCallback(CancellationToken token);

    internal interface IAchievementsProvider
    {
        event AchievementsUpdatedCallback OnAchievementsUpdates;
        Task<List<IAchievement>> GetLatestAchievements(
            int maxAchievemnets, 
            int maxAchievementsPerGame, 
            CancellationToken cancellationToken = default);
    }
}
