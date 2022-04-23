using LandingPage.ViewModels.Layout;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaktionslogik für GridNodeView.xaml
    /// </summary>
    public partial class GridNodeView : UserControl
    {
        public GridNodeView()
        {
            InitializeComponent();
        }

        private void Border_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (e.OriginalSource == sender && DataContext is GridNodeViewModel viewModel)
            {
                viewModel.Border_PreviewDragEnter(sender, e);
            } 
        }

        private void Border_PreviewDragLeave(object sender, DragEventArgs e)
        {
            if (e.OriginalSource == sender && DataContext is GridNodeViewModel viewModel)
            {
                viewModel.Border_PreviewDragExit(sender, e);
            } 
        }

        private void Border_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.OriginalSource == sender && DataContext is GridNodeViewModel viewModel)
            {
                viewModel.Border_PreviewMouseMove(sender, e);
            } 
        }
    }
}
