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

namespace LandingPage.Views
{
    /// <summary>
    /// Interaktionslogik für ClockView.xaml
    /// </summary>
    public partial class ClockView : UserControl
    {
        public ClockView()
        {
            InitializeComponent();

            ClockTextBlock.Loaded += ClockTextBlock_Loaded;

        }

        private void ClockTextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            var formattedText = new FormattedText(
                "01234:56789",
                System.Globalization.CultureInfo.CurrentCulture,
                System.Globalization.CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                new Typeface(ClockTextBlock.FontFamily, ClockTextBlock.FontStyle, ClockTextBlock.FontWeight, ClockTextBlock.FontStretch),
                ClockTextBlock.FontSize,
                Background,
                ClockTextBlock.FontSize);

            var baseline = formattedText.Height - formattedText.Baseline;

            //DateTransform.Y = -baseline;
            ClockTextBlock.Height = formattedText.Baseline;
            ClockTextBlock.Loaded -= ClockTextBlock_Loaded;
        }
    }
}
