using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.Models.Objects
{
    public class LoadingStatus : ObservableObject
    {
        private int total = 100;
        public int Total { get => total; set => SetValue(ref total, value); }
        private int current = 0;
        public int Current { get => current; set => SetValue(ref current, value); }
    }
}
