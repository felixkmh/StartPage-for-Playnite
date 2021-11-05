using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Helper
{
    public static class UiHelper
    {
        public static IEnumerable<FrameworkElement> FindVisualChildren(this DependencyObject parent, string name = null)
        {
            var queue = new Queue<DependencyObject>();
            queue.Enqueue(parent);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var count = VisualTreeHelper.GetChildrenCount(current);
                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(current, i);
                    if (child is FrameworkElement element)
                    {
                        if (string.IsNullOrEmpty(name) || element.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        {
                            yield return element;
                        }
                    }
                    queue.Enqueue(child);
                }
            }
        }

        public static IEnumerable<SearchType> FindVisualChildren<SearchType>(this DependencyObject parent, string name = null)
            where SearchType : FrameworkElement
        {
            return FindVisualChildren(parent, name).OfType<SearchType>();
        }
    }
}
