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
    public class GridNode : ObservableObjectExt
    {
        private GridLength size = new GridLength(1, GridUnitType.Star);
        public GridLength Size { get => size; set => SetValue(ref size, value); }

        private Orientation orientation;
        public Orientation Orientation { get => orientation; set => SetValue(ref orientation, value); }

        private HorizontalAlignment horizontalAlignment = HorizontalAlignment.Stretch;
        public HorizontalAlignment HorizontalAlignment { get => horizontalAlignment; set => SetValue(ref horizontalAlignment, value); }

        private VerticalAlignment verticalAlignment = VerticalAlignment.Stretch;
        public VerticalAlignment VerticalAlignment { get => verticalAlignment; set => SetValue(ref verticalAlignment, value); }

        private double padding = 0;
        public double Padding { get => padding; set => SetValue(ref padding, value); }

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
                parent.Orientation = current.Orientation;
                parent.ViewProperties = current.ViewProperties;
                parent.HorizontalAlignment = current.HorizontalAlignment;
                parent.VerticalAlignment = current.VerticalAlignment;
                parent.Padding = current.Padding;
                var children = new ObservableCollection<GridNode>();
                foreach(var child in current.Children)
                {
                    children.Add(child);
                }
                parent.Children = children;
            }
            if (current.Children.Count > 0)
            {
                current.ViewProperties = null;
            }
        }
    }
}
