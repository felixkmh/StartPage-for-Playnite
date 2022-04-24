using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage
{
    public class PropertyChangedExtendedEventArgs : PropertyChangedEventArgs
    {
        public virtual object OldValue { get; private set; }
        public virtual object NewValue { get; private set; }

        public PropertyChangedExtendedEventArgs(string propertyName, object oldValue,
               object newValue)
               : base(propertyName)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    public class ObservableObjectExt : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName, object oldvalue, object newvalue)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedExtendedEventArgs(propertyName, oldvalue, newvalue));
            }
        }

        protected void SetValue<T>(ref T property, T value, [CallerMemberName] string propertyName = null) 
        {
            var old = property;
            property = value;
            NotifyPropertyChanged(propertyName, old, value);
        }

        protected void SetValue<T>(ref T property, T value, params string[] propertyNames) 
        {
            var old = property;
            property = value;
            foreach (string name in propertyNames)
            {
                NotifyPropertyChanged(name, old, value);
            }
        }

    }
}
