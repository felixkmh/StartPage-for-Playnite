using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.Models
{
    internal interface IAchievement
    {
        string Name { get; }
        string Description { get; }
        bool IsUnlocked { get; }
        Uri Icon { get; }
        DateTime? UnlockedOn { get; }
        IGameAchievementsInfo GameInfo { get; }
    }
}
