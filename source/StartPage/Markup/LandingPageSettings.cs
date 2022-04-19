using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace LandingPage.Markup
{
    public class LandingPageSettings : MarkupExtension
    {
        public static Type GetTargetType(IProvideValueTarget provider)
        {
            var type = provider.TargetProperty?.GetType();
            if (type == null)
            {
                return typeof(Binding);
            }
            if (type == typeof(DependencyProperty))
            {
                type = ((DependencyProperty)provider.TargetProperty).PropertyType;
            }
            else if (typeof(PropertyInfo).IsAssignableFrom(provider.TargetProperty.GetType()))
            {
                type = ((PropertyInfo)provider.TargetProperty).PropertyType;
            }

            return type;
        }

        internal string path;
        public string Path { get => path; set => path = value; }

        public LandingPageSettings()
        {

        }

        public LandingPageSettings(string path)
        {
            this.path = path;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new Binding()
            {
                Source = LandingPageExtension.Instance.SettingsViewModel,
                Path = new PropertyPath(path),
                Mode = BindingMode.OneWay
            };

            var provider = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;

            var targetType = GetTargetType(provider);

            if (targetType == typeof(Visibility))
            {
                binding.Converter = Application.Current.Resources["BooleanToHiddenConverter"] as IValueConverter;
            } 

            return binding;
        }
    }
}
