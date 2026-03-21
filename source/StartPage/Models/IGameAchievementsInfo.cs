using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.Models
{
    internal interface IGameAchievementsInfo
    {
        int TotalAchievements { get; }
        int UnlockedAchievements { get; }
        double Progress { get; }
        string GameName { get; }
        Guid GameId { get; }
        DateTime? LastUnlock { get; }
    }
}
