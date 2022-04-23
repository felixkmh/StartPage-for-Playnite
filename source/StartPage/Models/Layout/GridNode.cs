using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using StartPage.SDK;
using System.Windows;
using Playnite.SDK.Data;

namespace LandingPage.Models.Layout
{
    public class GridNode : ObservableObject
    {
        private GridLength size = new GridLength(1, GridUnitType.Star);
        public GridLength Size { get => size; set => SetValue(ref size, value); }

        private Orientation orientation;
        public Orientation Orientation { get => orientation; set => SetValue(ref orientation, value); }

        private ViewProperties viewProperties = null;
        public ViewProperties ViewProperties { get => viewProperties; set => SetValue(ref viewProperties, value); }

        private ObservableCollection<GridNode> children = new ObservableCollection<GridNode>();
        public ObservableCollection<GridNode> Children { get => children; set => SetValue(ref children, value); }


        static public void Minimize(GridNode current, GridNode parent)
        {
            foreach(GridNode node in current.Children)
            {
                Minimize(node, current);
            }
            if (parent?.Children.Count == 1)
            {
                parent.Children = current.Children;
                parent.Orientation = current.Orientation;
                parent.ViewProperties = current.ViewProperties;
            }
            if (current.Children.Count > 0)
            {
                current.ViewProperties = null;
            }
        }
    }
}
