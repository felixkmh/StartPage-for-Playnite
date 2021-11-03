using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LandingPage.Models
{
    public class Clock : INotifyPropertyChanged
    {
        internal DispatcherTimer dispatcherTimer;

        public Clock()
        {
            dispatcherTimer = new DispatcherTimer(
                TimeSpan.FromSeconds(60 - (DateTime.Now.TimeOfDay.TotalSeconds % 60) + 0.01),
                DispatcherPriority.Normal,
                (sender, args) =>
                {
                    dispatcherTimer.Interval = TimeSpan.FromSeconds(60);
                    var currentTime = DateTime.Now;
                    if (currentTime != Time)
                    {
                        Time = currentTime;
                        OnPropertyChanged(nameof(Time));
                    }
                },
                Application.Current.Dispatcher
            );
        }

        public DateTime Time { get; set; } = DateTime.Now;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
