using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.Extensions
{
    public static class ObservableCollectionExtensions
    {
        public static void UpdateOrAdd<T>(this ObservableCollection<T> collection, T item, int index)
        {
            if (collection != null)
            {
                if (collection.Count > index)
                {
                    collection[index] = item;
                } else
                {
                    collection.Add(item);
                }
            }
        }

        public static void Update<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            if (collection != null)
            {
                int i = 0;
                foreach(var item in items)
                {
                    collection.UpdateOrAdd(item, i);
                    ++i;
                }
                for (int j = collection.Count - 1; j >= i; --j)
                {
                    collection.RemoveAt(j);
                }
            }
        }
    }
}
