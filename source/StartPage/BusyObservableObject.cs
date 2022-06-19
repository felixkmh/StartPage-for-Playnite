using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Playnite.SDK;

namespace LandingPage
{
    public class BusyObservableObject : ObservableObject
    {
        private bool isBusy = false;
        public bool IsBusy { get => isBusy; set { SetValue(ref isBusy, value); OnPropertyChanged(nameof(IsNotBusy)); } }

        public bool IsNotBusy => !IsBusy;
    }
}
