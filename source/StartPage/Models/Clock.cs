using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        internal DispatcherTimer leetTimer;
        internal double elapsedSeconds = 0;
        private Random random = new Random();
        private Regex leetRegex = new Regex(@"13.37");

        public Clock()
        {
            TimeString = GetTimeString();
            dispatcherTimer = new DispatcherTimer(
                TimeSpan.FromSeconds(60 - (DateTime.Now.TimeOfDay.TotalSeconds % 60)),
                DispatcherPriority.Normal,
                (sender, args) =>
                {
                    dispatcherTimer.Interval = TimeSpan.FromSeconds(60 - (DateTime.Now.TimeOfDay.TotalSeconds % 60));
                    var currentTime = DateTime.Now.RoundToClosestMinute();
                    if (currentTime != Time)
                    {
                        if (currentTime.DayOfYear != Time.DayOfYear)
                        {
                            OnDayChanged(EventArgs.Empty);
                        }
                        Time = currentTime;
                        TimeString = GetTimeString();
                        var leet = leetRegex.IsMatch(TimeString);
                        if (!LeetTime && leet)
                        {
                            leetTimer = new DispatcherTimer(DispatcherPriority.Render, Application.Current.Dispatcher);
                            leetTimer.Interval = TimeSpan.FromMilliseconds(25);
                            leetTimer.Tick += LeetTimer_Tick;
                            leetTimer.Start();
                        }
                        LeetTime = leet; 
                        OnPropertyChanged(nameof(Time));
                        OnPropertyChanged(nameof(DateString));
                        OnPropertyChanged(nameof(Separator));
                        OnPropertyChanged(nameof(T));
                    }
                },
                Application.Current.Dispatcher
            );
        }

        private void LeetTimer_Tick(object sender, EventArgs e)
        {
            elapsedSeconds += leetTimer.Interval.TotalSeconds;
            if (elapsedSeconds < 1)
            {
                var display = TimeString;
                var r = random.Next(33, 127);
                var pos = random.Next(display.Length);
                var c = (char)r;
                var sb = new StringBuilder();
                for (int i = 0; i < display.Length; ++i)
                {
                    if (i == pos)
                    {
                        sb.Append(c);
                    } else
                    {
                        sb.Append(display[i]);
                    }
                }
                TimeString = sb.ToString();
            } else
            {
                var display = TimeString;
                var time = GetTimeString();
                var pos = random.Next(display.Length);
                var c = time[pos];
                var sb = new StringBuilder();
                for (int i = 0; i < display.Length; ++i)
                {
                    if (i == pos)
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        sb.Append(display[i]);
                    }
                }
                TimeString = sb.ToString();
                if (TimeString == time)
                {
                    leetTimer.Stop();
                    leetTimer = null;
                    elapsedSeconds = 0;
                    TimeString = GetTimeString();
                    LeetTime = false;
                }
            }
        }

        private bool leetTime = false;
        public bool LeetTime { get => leetTime; set { leetTime = value; OnPropertyChanged(nameof(LeetTime)); } }

        public event EventHandler DayChanged;

        protected virtual void OnDayChanged(EventArgs e)
        {
            DayChanged?.Invoke(this, e);
        }

        private string GetTimeString()
        {
            DateTimeFormatInfo currentInfo = DateTimeFormatInfo.CurrentInfo;
            var format = currentInfo.ShortTimePattern.Replace("t", string.Empty).Trim();
            return Time.ToString(format);
        }

        public DateTime Time { get; set; } = DateTime.Now;


        private string timeString;
        public string TimeString { get => timeString; set { timeString = value; OnPropertyChanged(nameof(TimeString)); } }

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
