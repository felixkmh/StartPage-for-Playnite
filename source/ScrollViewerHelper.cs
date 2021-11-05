using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Helper;

namespace LandingPage
{
    public static class ScrollViewerHelper
    {
        public static readonly DependencyProperty UseHorizontalScrollingProperty = DependencyProperty.RegisterAttached(
            "UseHorizontalScrolling", typeof(bool), typeof(ScrollViewerHelper), new PropertyMetadata(default(bool), UseHorizontalScrollingChangedCallback));

        private static void UseHorizontalScrollingChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            ItemsControl itemsControl = dependencyObject as ItemsControl;

            if (itemsControl == null) throw new ArgumentException("Element is not an ItemsControl");

            itemsControl.PreviewMouseWheel += delegate (object sender, MouseWheelEventArgs args)
            {
                ScrollViewer scrollViewer = itemsControl.FindVisualChildren<ScrollViewer>().FirstOrDefault();

                if (scrollViewer == null) return;

                double? scrollWidth = null;

                if (itemsControl.ItemContainerGenerator.ContainerFromIndex(5) is FrameworkElement container)
                {
                    scrollWidth = container.DesiredSize.Width;
                }

                if (scrollWidth is double delta)
                {
                    scrollViewer.ScrollToHorizontalOffset(Math.Round(Math.Max(0, scrollViewer.HorizontalOffset - delta * Math.Sign(args.Delta))));
                } else
                {
                    if (args.Delta < 0)
                    {
                        scrollViewer.LineLeft();
                    }
                    else
                    {
                        scrollViewer.LineRight();
                    }
                }

                args.Handled = true;
            };
        }


        public static void SetUseHorizontalScrolling(ItemsControl element, bool value)
        {
            element.SetValue(UseHorizontalScrollingProperty, value);
        }

        public static bool GetUseHorizontalScrolling(ItemsControl element)
        {
            return (bool)element.GetValue(UseHorizontalScrollingProperty);
        }
    }
}
