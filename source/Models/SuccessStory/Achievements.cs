using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.Models.SuccessStory
{
    public class Achievements
    {
        public List<Achivement> Items { get; set; } = new List<Achivement>();
        public bool HaveAchivements { get; set; }
        public int Total { get; set; }
        public int Unlocked { get; set; }
        public int Locked { get; set; }
        public int Progression { get; set; }
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class Achivement
    {
        public string Name { get; set; }
        public string ApiName { get; set; } = string.Empty;
        public string Description { get; set; }
        public string UrlUnlocked { get; set; }
        public string UrlLocked { get; set; }
        public DateTime? DateUnlocked { get; set; }
        public bool IsHidden { get; set; } = false;
        public float Percent { get; set; } = 100;
        public string Category { get; set; } = string.Empty;
        public string ParentCategory { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonIgnore]
        public Uri UriUnlocked => new Uri(UrlUnlocked);
    }
}
