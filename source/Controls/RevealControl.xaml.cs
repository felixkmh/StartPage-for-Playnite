using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LandingPage.Controls
{
    /// <summary>
    /// Interaktionslogik für RevealControl.xaml
    /// </summary>
    public partial class RevealControl : ContentControl
    {
        public RevealControl()
        {
            InitializeComponent();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            e.Handled = true;
            base.OnPreviewKeyDown(e);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            e.Handled = true;
            base.OnPreviewMouseDown(e);
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            e.Handled = true;
            base.OnPreviewMouseMove(e);
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            e.Handled = true;
            base.OnPreviewMouseWheel(e);
        }

        protected override void OnPreviewTouchDown(TouchEventArgs e)
        {
            e.Handled = true;
            base.OnPreviewTouchDown(e);
        }

        protected override void OnPreviewTouchMove(TouchEventArgs e)
        {
            e.Handled = true;
            base.OnPreviewTouchMove(e);
        }
    }
}
