using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaktionslogik für SidewaysTextBlock.xaml
    /// </summary>
    public partial class SidewaysTextBlock : UserControl
    {
        public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
        public static DependencyProperty TextProperty
            = DependencyProperty.Register(nameof(Text), typeof(string), typeof(SidewaysTextBlock), new PropertyMetadata(string.Empty, TextChangedCallback));

        private static void TextChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SidewaysTextBlock stb)
            {
                stb.OnTextChanged((string)e.OldValue, (string)e.NewValue);
            }
        }

        RotateTransform rotateTransform = new RotateTransform(-90);

        private static readonly Regex cjkCharRegex = new Regex(@"\p{IsCJKUnifiedIdeographs}");
        public static bool IsCJK(char c)
        {
            return cjkCharRegex.IsMatch(c.ToString());
        }

        private class SubString
        {
            public bool IsCJK = false;
            public string S = string.Empty;
        }

        private List<SubString> SplitText(string input)
        {
            var result = new List<SubString>();
            char prev = default;
            var sb = new StringBuilder();
            foreach(var c in input)
            {
                if (IsCJK(c) != IsCJK(prev) && prev != default)
                {
                    var s = sb.ToString();
                    if (s.Length > 0)
                    {
                        result.Add(new SubString { S = s, IsCJK = IsCJK(prev) });
                        sb.Clear();
                    }
                } 

                sb.Append(c);

                prev = c;
            }
            var last = sb.ToString();
            if (last.Length > 0)
            {
                result.Add(new SubString { S = last, IsCJK = IsCJK(last[0]) });
                sb.Clear();
            }
            return result;
        }

        protected void OnTextChanged(string oldText, string newText)
        {
            CharacterStackPanel.Children.Clear();
            var split = SplitText(newText);
            var isMixed = split.Any(s => s.IsCJK) && split.Any(s => !s.IsCJK && !string.IsNullOrWhiteSpace(s.S));
            if (isMixed)
            {
                rotateTransform.Angle = 90;
            }
            var reverse = new List<TextBlock>();
            foreach(var subString in split)
            {
                var tb = new TextBlock() { Text = subString.S, Padding = new Thickness(0), Margin = new Thickness(0) };
                if (subString.IsCJK)
                {
                    tb.SetBinding(TextBlock.MaxWidthProperty, new Binding("FontSize") { Source = tb, Mode = BindingMode.OneWay });
                    tb.TextWrapping = TextWrapping.Wrap;
                } else
                {
                    tb.LayoutTransform = rotateTransform;
                }
                CharacterStackPanel.Children.Add(tb);
            }
        }

        public SidewaysTextBlock()
        {
            InitializeComponent();
        }
    }
}
