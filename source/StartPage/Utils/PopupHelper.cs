using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Threading;

namespace LandingPage.Utils
{
    public static class PopupHelper
    {
        public static readonly DependencyProperty IsNotTopmostProperty =
            DependencyProperty.RegisterAttached(
                "IsNotTopmost",
                typeof(bool),
                typeof(PopupHelper),
                new PropertyMetadata(false, OnIsNotTopmostChanged));

        public static void SetIsNotTopmost(DependencyObject element, bool value)
            => element.SetValue(IsNotTopmostProperty, value);

        public static bool GetIsNotTopmost(DependencyObject element)
            => (bool)element.GetValue(IsNotTopmostProperty);

        private static void OnIsNotTopmostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is Popup popup))
                return;

            if ((bool)e.NewValue)
            {
                popup.Opened -= PopupOnOpened;
                popup.Opened += PopupOnOpened;
            }
            else
            {
                popup.Opened -= PopupOnOpened;
            }
        }

        private static void PopupOnOpened(object sender, EventArgs e)
        {
            if (!(sender is Popup popup))
                return;

            // Delay to ensure the hwnd is created
            popup.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                var source = PresentationSource.FromVisual(popup.Child) as HwndSource;
                if (source == null)
                    return;

                var hwnd = source.Handle;
                const int SWP_NOSIZE = 0x0001;
                const int SWP_NOMOVE = 0x0002;
                const int SWP_NOACTIVATE = 0x0010;
                var HWND_NOTOPMOST = new IntPtr(-2);

                SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }));
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            int uFlags);
    }


}
