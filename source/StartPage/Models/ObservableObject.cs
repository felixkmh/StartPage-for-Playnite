using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.Models
{
    public class ObservableObject<T> : INotifyPropertyChanged
    {
        internal T _obj = default;

        public T Object
        {
            get { return _obj; }
            set
            {
                if (_obj?.Equals(value) ?? value == null) return;
                _obj = value;
                OnPropertyChanged(nameof(Object));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
