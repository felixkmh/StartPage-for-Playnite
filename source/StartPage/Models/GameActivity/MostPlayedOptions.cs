using LandingPage.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.Models.GameActivity
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum Timeframe
    {
        [Description("LOC_SPG_TimeframeNone")]
        None,
        [Description("LOC_SPG_TimeframeLastDay")]
        LastDay,
        [Description("LOC_SPG_TimeframeLast7Days")]
        Last7Days,
        [Description("LOC_SPG_TimeframeLast14Days")]
        Last14Days,
        [Description("LOC_SPG_TimeframeLast21Days")]
        Last21Days,
        [Description("LOC_SPG_TimeframeLastMonth")]
        LastMonth,
        [Description("LOC_SPG_TimeframeLast3Months")]
        Last3Month,
        [Description("LOC_SPG_TimeframeLast6Months")]
        Last6Month,
        [Description("LOC_SPG_TimeframeLastYear")]
        LastYear,
        [Description("LOC_SPG_TimeframeLast5Years")]
        Last5Years,
        [Description("LOC_SPG_TimeframeAllTime")]
        AllTime
    }

    public class MostPlayedOptions : ObservableObject
    {
        public static readonly Dictionary<Timeframe, TimeSpan> TimeframeToTimespan
        = new Dictionary<Timeframe, TimeSpan>
        {
            { Timeframe.None,       TimeSpan.FromDays(0)       },
            { Timeframe.LastDay,    TimeSpan.FromDays(1)       },
            { Timeframe.Last7Days,  TimeSpan.FromDays(7)       },
            { Timeframe.Last14Days, TimeSpan.FromDays(14)      },
            { Timeframe.Last21Days, TimeSpan.FromDays(21)      },
            { Timeframe.LastMonth,  TimeSpan.FromDays(31)      },
            { Timeframe.Last3Month, TimeSpan.FromDays(93)      },
            { Timeframe.Last6Month, TimeSpan.FromDays(6 * 31)  },
            { Timeframe.LastYear,   TimeSpan.FromDays(365)     },
            { Timeframe.Last5Years, TimeSpan.FromDays(365 * 5) },
            { Timeframe.AllTime,    TimeSpan.MaxValue          }
        };

        private Timeframe timeframe;
        public Timeframe Timeframe { get => timeframe; set => SetValue(ref timeframe, value); }

        public TimeSpan TimeSpan => TimeframeToTimespan[Timeframe];
    }
}
