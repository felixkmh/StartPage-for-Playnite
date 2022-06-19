using LandingPage.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.Settings
{
    public class ShelvesSettings : ObservableObject
    {
        private ObservableCollection<ShelveProperties> shelveProperties = new ObservableCollection<ShelveProperties>();
        public ObservableCollection<ShelveProperties> ShelveProperties { get => shelveProperties; set => SetValue(ref shelveProperties, value); }

        private bool horizontalLabels = false;
        public bool HorizontalLabels { get => horizontalLabels; set => SetValue(ref horizontalLabels, value); }
    }
}
