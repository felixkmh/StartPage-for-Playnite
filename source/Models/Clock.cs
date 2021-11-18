using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
            var hours = Hour;
            dispatcherTimer = new DispatcherTimer(
                TimeSpan.FromSeconds(60 - (DateTime.Now.TimeOfDay.TotalSeconds % 60) + 0.01),
                DispatcherPriority.Normal,
                (sender, args) =>
                {
                    dispatcherTimer.Interval = TimeSpan.FromSeconds(60);
                    //var currentCulture = CultureInfo.CurrentCulture;
                    //CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                    var currentTime = DateTime.Now;
                    if (currentTime != Time)
                    {
                        Time = currentTime;
                        if (currentTime.DayOfYear != Time.DayOfYear)
                        {
                            OnDayChanged(EventArgs.Empty);
                        }
                        OnPropertyChanged(nameof(Time));
                        OnPropertyChanged(nameof(DateString));
                        OnPropertyChanged(nameof(Hour));
                        OnPropertyChanged(nameof(Minute));
                        OnPropertyChanged(nameof(Separator));
                        OnPropertyChanged(nameof(T));
                    }
                    //CultureInfo.CurrentCulture = currentCulture;
                },
                Application.Current.Dispatcher
            );
        }

        public event EventHandler DayChanged;

        protected virtual void OnDayChanged(EventArgs e)
        {
            DayChanged?.Invoke(this, e);
        }

        public DateTime Time { get; set; } = DateTime.Now;

        public string Hour 
        { 
            get
            {
                if (!string.IsNullOrEmpty(DateTimeFormatInfo.CurrentInfo.AMDesignator) || !string.IsNullOrEmpty(DateTimeFormatInfo.CurrentInfo.PMDesignator))
                {
                    return Time.ToString("hh");
                } else
                {
                    return Time.ToString("HH");
                }
            } 
        }

        public string Minute
        {
            get
            {
                return Time.ToString("mm");
            }
        }

        public string Separator
        {
            get
            {
                return DateTimeFormatInfo.CurrentInfo.TimeSeparator;
            }
        }

        public string T
        {
            get
            {
                return Time.ToString("tt");
            }
        }

        public string DateString => Time.ToLongDateString();

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
