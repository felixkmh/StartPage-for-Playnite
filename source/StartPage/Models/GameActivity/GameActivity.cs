using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.Models.GameActivity
{
    public class Activity : ObservableObject
    {
        private List<Session> items = new List<Session>();
        private Guid id = default;
        private string name = string.Empty;

        public List<Session> Items { get => items; set => SetValue(ref items, value); }
        public Guid Id { get => id; set => SetValue(ref id, value); }
        public string Name { get => name; set => SetValue(ref name, value); }
    }

    public class Session : ObservableObject
    {
        private Guid sourceID = default;
        private Guid platformID = default;
        private List<Guid> platformIDs = new List<Guid>();
        private int idConfiguration = 0;
        private DateTime dateSession = default;
        private ulong elapsedSeconds = 0;

        public Guid SourceID { get => sourceID; set => SetValue(ref sourceID, value); }
        public Guid PlatfromID { get => platformID; set => SetValue(ref platformID, value); }
        public List<Guid> PlatformIDs { get => platformIDs; set => SetValue(ref platformIDs, value); }
        public int IdConfiguration { get => idConfiguration; set => SetValue(ref idConfiguration, value); }
        public DateTime DateSession { get => dateSession; set => SetValue(ref dateSession, value); }
        public ulong ElapsedSeconds { get => elapsedSeconds; set => SetValue(ref elapsedSeconds, value); }
    }
}
