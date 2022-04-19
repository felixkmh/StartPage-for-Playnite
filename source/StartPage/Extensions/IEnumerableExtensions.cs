using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.Extensions
{
    public static class IEnumerableExtensions
    {
        public static KeyValuePair<double, TValue> MaxElement<TValue>(this IEnumerable<TValue> enumerable, Func<TValue, double> func)
        {
            var max = double.NegativeInfinity;
            TValue maxValue = default;
            foreach(var item in enumerable)
            {
                var score = func(item);
                if (score > max)
                {
                    max = score;
                    maxValue = item;
                }
            }
            return new KeyValuePair<double, TValue>(max, maxValue);
        }

        public static TValue MaxElement<TValue>(this IEnumerable<TValue> enumerable, Func<TValue, TValue, int> compare)
        {

            TValue maxValue = default;
            foreach (var item in enumerable)
            {
                if (compare(maxValue, item) > 0)
                {
                    maxValue = item;
                }
            }
            return maxValue;
        }
    }
}
