using System;
using System.Collections;
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
    /// Interaktionslogik für GroupListBox.xaml
    /// </summary>
    public partial class GroupItemsControl : ItemsControl
    {
        public GroupItemsControl()
        {
            InitializeComponent();
            CollectionViewSource collectionViewSource = new CollectionViewSource() { };
            ItemsSource = (IEnumerable)collectionViewSource;
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {

            base.OnItemsSourceChanged(oldValue, newValue);
        }

        
    }
}
