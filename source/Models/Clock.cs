using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
                        OnPropertyChanged(nameof(Separator));
                        OnPropertyChanged(nameof(T));
                        OnPropertyChanged(nameof(TimeString));
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

        public string TimeString
        {
            get
            {
                DateTimeFormatInfo currentInfo = DateTimeFormatInfo.CurrentInfo;
                var format = currentInfo.ShortTimePattern.Replace("t", string.Empty).Trim();
                return Time.ToString(format);
            }
        }

        public string Separator
        {
            get
            {
                return DateTimeFormatInfo.CurrentInfo.TimeSeparator;
            }
        }

        private static readonly Regex tPattern = new Regex(@"t+", RegexOptions.IgnoreCase);
        public string T
        {
            get
            {
                DateTimeFormatInfo currentInfo = DateTimeFormatInfo.CurrentInfo;
                var format = tPattern.Match(currentInfo.ShortTimePattern)?.Value ?? string.Empty;
                if (string.IsNullOrEmpty(format))
                {
                    return string.Empty;
                }
                return Time.ToString(format);
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
