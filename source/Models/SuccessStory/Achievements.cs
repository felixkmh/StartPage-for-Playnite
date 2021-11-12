using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.Models.SuccessStory
{
    public class Achievements : ObservableObject
    {
        private List<Achivement> items = new List<Achivement>();
        private bool haveAchivements;
        private int total;
        private int unlocked;
        private int locked;
        private int progression;
        private Guid id;
        private string name;

        public List<Achivement> Items { get => items; set { items = value; OnPropertyChanged(); } }
        public bool HaveAchivements { get => haveAchivements; set { haveAchivements = value; OnPropertyChanged(); } }
        public int Total { get => total; set { total = value; OnPropertyChanged(); } }
        public int Unlocked { get => unlocked; set { unlocked = value; OnPropertyChanged(); } }
        public int Locked { get => locked; set { locked = value; OnPropertyChanged(); } }
        public int Progression { get => progression; set { progression = value; OnPropertyChanged(); } }
        public Guid Id { get => id; set { id = value; OnPropertyChanged(); } }
        public string Name { get => name; set { name = value; OnPropertyChanged(); } }

        public DateTime LastUnlocked => items.OrderByDescending(i => i.DateUnlocked).Select(i => i.DateUnlocked).FirstOrDefault() ?? default;

        public override string ToString()
        {
            return Name;
        }
    }

    public class Achivement : ObservableObject
    {
        private string name;
        private string apiName = string.Empty;
        private string description;
        private string urlUnlocked;
        private string urlLocked;
        private DateTime? dateUnlocked;
        private bool isHidden = false;
        private float percent = 100;
        private string category = string.Empty;
        private string parentCategory = string.Empty;

        public string Name { get => name; set { name = value; OnPropertyChanged(); } }
        public string ApiName { get => apiName; set { apiName = value; OnPropertyChanged(); } }
        public string Description { get => description; set { description = value; OnPropertyChanged(); } }
        public string UrlUnlocked { get => urlUnlocked; set { urlUnlocked = value; OnPropertyChanged(); OnPropertyChanged(nameof(UriUnlocked)); } }
        public string UrlLocked { get => urlLocked; set { urlLocked = value; OnPropertyChanged(); } }
        public DateTime? DateUnlocked { get => dateUnlocked ?? DateTime.MinValue; set { dateUnlocked = value; OnPropertyChanged(); } }
        public bool IsHidden { get => isHidden; set { isHidden = value; OnPropertyChanged(); } }
        public float Percent { get => percent; set { percent = value; OnPropertyChanged(); } }
        public string Category { get => category; set { category = value; OnPropertyChanged(); } }
        public string ParentCategory { get => parentCategory; set { parentCategory = value; OnPropertyChanged(); } }
        [Newtonsoft.Json.JsonIgnore]
        public Uri UriUnlocked => string.IsNullOrEmpty(UrlUnlocked) ? null : new Uri(UrlUnlocked);
    }
}
