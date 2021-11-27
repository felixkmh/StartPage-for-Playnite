using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.Models.GameActivity
{
    public class PlayTimePerSource : ObservableObject
    {
        private GameSource gameSource;
        public GameSource GameSource { get => gameSource; set => SetValue(ref gameSource, value); }

        private long playtime;
        public long Playtime { get => playtime; set => SetValue(ref playtime, value); }
    }
}
