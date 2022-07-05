using LandingPage.Models.Layout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using LandingPage.Views;
using System.Collections.Specialized;
using StartPage.SDK;
using System.Windows.Input;
using System.Windows.Data;
using Playnite.SDK;
using System.Windows.Shell;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace LandingPage.ViewModels.Layout
{
    public class GridNodeViewModel : ObservableObject
    {
        private GridNode gridNode;
        public GridNode GridNode { get => gridNode; set => SetValue(ref gridNode, value); }

        private bool editModeEnabled = false;
        public bool EditModeEnabled
        {
            get => editModeEnabled;
            set {
                SetValue(ref editModeEnabled, value);
                foreach (FrameworkElement child in View.Children)
                {
                    if (child.DataContext is GridNodeViewModel model)
                    {
                        if (model.EditModeEnabled != value)
                        {
                            model.EditModeEnabled = value;
                        }
                    }
                }
            }
        }

        private bool isLeaf = false;
        public bool IsLeaf { get => isLeaf; set => SetValue(ref isLeaf, value); }

        private bool hasView = false;
        public bool HasView { get => hasView; set => SetValue(ref hasView, value); }

        private Grid view = new Grid();
        public Grid View { get => view; set => SetValue(ref view, value); }

        public ICommand AddCommand { get; private set; }
        public ICommand SplitHorizontallyCommand { get; private set; }
        public ICommand SplitVerticallyCommand { get; private set; }
        public ICommand RemoveViewCommand { get; private set; }
        public ICommand RemovePanelCommand { get; private set; }
        public ICommand MergeWithPreviousPanelCommand { get; private set; }
        public ICommand MergeWithNextPanelCommand { get; private set; }
        public ICommand SetVerticalAlignmentCommand { get; private set; }
        public ICommand SetHorizontalAlignmentCommand { get; private set; }
        public ICommand OpenWikiCommand { get; private set; }

        public Control ViewSettings
        { 
            get 
            { 
                if (GridNode.ViewProperties?.StartPageViewArgs?.HasSettings ?? false)
                {
                    var plugin = LandingPageExtension.Instance.PlayniteApi.Addons.Plugins.FirstOrDefault(p => p.Id == GridNode.ViewProperties.PluginId);
                    if (plugin is IStartPageExtension extension)
                    {
                        return extension.GetStartPageViewSettings(GridNode.ViewProperties.ViewId, GridNode.ViewProperties.InstanceId);
                    }
                }
                return null;
            } 
        }

        public GridNodeViewModel Parent { get; internal set; } = null;

        public GridNodeViewModel Root 
        { 
            get
            {
                var current = this;
                while(current.Parent != null)
                {
                    current = current.Parent;
                }
                return current;
            } 
        }

        public IEnumerable<ViewProperties> ActiveViews
        {
            get
            {
                var root = Root;
                var stack = new Stack<GridNodeViewModel>();
                stack.Push(root);
                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    if (current.GridNode.ViewProperties != null)
                    {
                        yield return current.GridNode.ViewProperties;
                    }
                    if (current.GridNode.Children.Any())
                    {
                        var children = current.View.Children.OfType<FrameworkElement>().Select(fe => fe.DataContext).OfType<GridNodeViewModel>();
                        foreach(var child in children)
                        {
                            if (child != current)
                            {
                                stack.Push(child);
                            }
                        }
                    }
                }
            }
        }

        public class AvailableView
        {
            public GridNodeViewModel Model { get; private set; }
            public StartPageViewArgs ViewArgs { get; private set; }

            public static ICommand AddCommand { get; } = new RelayCommand<AvailableView>(
                v => {
                    v.Model.RemoveCurrentView();
                    v.Model.GridNode.ViewProperties = new ViewProperties { PluginId = v.ViewArgs.PluginId, StartPageViewArgs = v.ViewArgs, ViewId = v.ViewArgs.ViewId };
                },
                v =>
                {
                    return v.ViewArgs.AllowMultipleInstances || !v.Model.ActiveViews.Any(vp => vp.PluginId == v.ViewArgs.PluginId && vp.ViewId == v.ViewArgs.ViewId);
                });

            public AvailableView(GridNodeViewModel model, StartPageViewArgs args)
            {
                ViewArgs = args;
                Model = model;
            }
        }

        public Dictionary<string, IEnumerable<AvailableView>> AvailableViews
            => LandingPageExtension.Instance.AllAvailableViews.ToDictionary(p => p.Key, p => p.Value.Select(v => new AvailableView(this, v)));

        public GridNodeViewModel(GridNode node)
        {
            gridNode = node;
            node.Children.CollectionChanged += Children_CollectionChanged;
            node.PropertyChanged += Node_PropertyChanged;

            AddCommand = new Playnite.SDK.RelayCommand(() =>
            {
                if (GridNode.Children.Count == 0) 
                {
                    GridNode.Children.Add(new GridNode());
                } 
                GridNode.Children.Add(new GridNode());
            });
            SplitHorizontallyCommand = new Playnite.SDK.RelayCommand(() =>
            {
                Split(Orientation.Horizontal);

            });
            SplitVerticallyCommand = new Playnite.SDK.RelayCommand(() =>
            {
                Split(Orientation.Vertical);

            });

            RemoveViewCommand = new RelayCommand(RemoveCurrentView);
            RemovePanelCommand = new RelayCommand(RemovePanel, () => Parent != null);

            MergeWithNextPanelCommand = new RelayCommand(MergeWithNextPanel, 
                () => Parent != null && Parent.GridNode.Children.IndexOf(GridNode) < Parent.GridNode.Children.Count - 1);

            MergeWithPreviousPanelCommand = new RelayCommand(MergeWithPreviousPanel,
                () => Parent != null && Parent.GridNode.Children.IndexOf(GridNode) > 0);

            SetHorizontalAlignmentCommand = new RelayCommand<HorizontalAlignment>(SetHorizontalAlignment);
            SetVerticalAlignmentCommand = new RelayCommand<VerticalAlignment>(SetVerticalAlignment);

            OpenWikiCommand = new RelayCommand(() => Process.Start("https://github.com/felixkmh/StartPage-for-Playnite/wiki/Integrating-with-other-Extensions"));

            CreateView(node);
        }

        public void SetHorizontalAlignment(HorizontalAlignment align)
        {
            GridNode.HorizontalAlignment = align;
        }

        public void SetVerticalAlignment(VerticalAlignment align)
        {
            GridNode.VerticalAlignment = align;
        }

        public void MergeWithPreviousPanel()
        {
            if (Parent != null)
            {
                var root = Root.GridNode;
                var index = Parent.GridNode.Children.IndexOf(GridNode);
                if (index > 0)
                {
                    var removed = Parent.GridNode.Children[index - 1];
                    Parent.GridNode.Children.RemoveAt(index - 1);
                }
                GridNode.Minimize(root, null);
            }
        }

        public void MergeWithNextPanel()
        {
            if (Parent != null)
            {
                var root = Root.GridNode;
                var index = Parent.GridNode.Children.IndexOf(GridNode);
                if (index > -1 && index < Parent.GridNode.Children.Count - 1)
                {
                    var removed = Parent.GridNode.Children[index + 1];
                    Parent.GridNode.Children.RemoveAt(index + 1);
                }
                GridNode.Minimize(root, null);
            }
        }

        private void RemovePanel()
        {
            if (Parent != null)
            {
                var root = Root.GridNode;
                var index = Parent.GridNode.Children.IndexOf(GridNode);
                var siblings = new List<GridNode>();
                if (index > 0) 
                    siblings.Add(Parent.GridNode.Children[index - 1]);
                if (index < Parent.GridNode.Children.Count - 1)
                    siblings.Add(Parent.GridNode.Children[index + 1]);
                siblings.ForEach(s => s.Size = new GridLength(s.Size.Value + GridNode.Size.Value / siblings.Count, GridUnitType.Star));
                Parent.GridNode.Children.Remove(GridNode);
                var properties = GridNode.ViewProperties;
                if (properties != null)
                {
                    GridNode.ViewProperties.view = null;
                    GridNode.ViewProperties = null;
                }
                View.Children.Clear();
                GridNode.Minimize(root, null);
            }
        }

        static public void OnNodeRemoved(GridNode removed)
        {
            foreach(var child in removed.Children)
            {
                OnNodeRemoved(child);
            }
            var properties = removed.ViewProperties;
            if (properties != null)
            {
                //Debug.WriteLine($"Removed view {removed.ViewProperties.ViewId}, {removed.ViewProperties.InstanceId}.");
                var plugin = LandingPageExtension.Instance.PlayniteApi.Addons.Plugins.FirstOrDefault(p => p.Id == properties.PluginId);
                if (plugin is IStartPageExtension extension)
                {
                    try
                    {
                        extension.OnViewRemoved(properties.ViewId, properties.InstanceId);
                    }
                    catch (Exception ex)
                    {
                        LandingPageExtension.logger.Warn
                            (
                                ex,
                                $"Error when calling OnViewRemoved() on extension {plugin.GetType().Name} with viewId {properties.ViewId} and instanceId {properties.InstanceId}."
                            );
                    }
                }
            }

        }

        private void RemoveCurrentView()
        {
            var properties = GridNode.ViewProperties;
            if (properties != null)
            {
                GridNode.ViewProperties.view = null;
                GridNode.ViewProperties = null;
                var plugin = LandingPageExtension.Instance.PlayniteApi.Addons.Plugins.FirstOrDefault(p => p.Id == properties.PluginId);
                if (plugin is IStartPageExtension extension)
                {
                    try
                    {
                        extension.OnViewRemoved(properties.ViewId, properties.InstanceId);
                    }
                    catch (Exception ex)
                    {
                        LandingPageExtension.logger.Warn
                            (
                                ex,
                                $"Error when calling OnViewRemoved() on extension {plugin.GetType().Name} with viewId {properties.ViewId} and instanceId {properties.InstanceId}."
                            );
                    }
                }
            }
            View.Children.Clear();
            GridNode.ViewProperties = null;
            GC.Collect();
        }

        public void Border_PreviewDrop(object sender, DragEventArgs e)
        {
            
        }

        private static void SwapViewProperties(GridNodeViewModel source, GridNodeViewModel target)
        {
            if (source.GridNode.ViewProperties?.view is UIElement element)
            {
                source.View.Children.Remove(element);
            }
            if (target.GridNode.ViewProperties?.view is UIElement element2)
            {
                target.View.Children.Remove(element2);
            }
            var tempVerticalAlignemnt = source.GridNode.VerticalAlignment;
            var tempHorizontalAlignemnt = source.GridNode.HorizontalAlignment;
            var tempPadding = source.GridNode.Padding;
            source.GridNode.VerticalAlignment = target.GridNode.VerticalAlignment;
            source.GridNode.HorizontalAlignment = target.GridNode.HorizontalAlignment;
            source.GridNode.Padding = target.GridNode.Padding;
            target.GridNode.VerticalAlignment = tempVerticalAlignemnt;
            target.GridNode.HorizontalAlignment = tempHorizontalAlignemnt;
            target.GridNode.Padding = tempPadding;
            var temp = source.GridNode.ViewProperties;
            source.GridNode.ViewProperties = target.GridNode.ViewProperties;
            target.GridNode.ViewProperties = temp;
        }

        public void Border_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetData("DragSource") is GridNodeViewModel source)
            {
                if (source != this)
                {
                    SwapViewProperties(this, source);
                }
            }
        }

        public void Border_PreviewDragExit(object sender, DragEventArgs e)
        {
            if (e.Data.GetData("DragSource") is GridNodeViewModel source)
            {
                if (source != this)
                {
                    SwapViewProperties(this, source);
                }
            }
        }

        public void Border_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.OriginalSource is Border border)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    var data = new DataObject();
                    data.SetData("DragSource", this);
                    DragDrop.DoDragDrop(border, data, DragDropEffects.All);
                }
            }
        }

        private void Node_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GridNode.Children))
            {
                if (e is PropertyChangedExtendedEventArgs eventArgs && eventArgs.OldValue is ObservableCollection<GridNode> oldValue)
                {
                    oldValue.CollectionChanged -= Children_CollectionChanged;
                }
                CreateView(GridNode);
                GridNode.Children.CollectionChanged += Children_CollectionChanged;
                IsLeaf = GridNode.Children.Count == 0;
            }
            if (e.PropertyName == nameof(GridNode.ViewProperties))
            {
                var hasView = false;
                if (GridNode.Children.Count == 0)
                {
                    View.Children.Clear();
                    if (gridNode.ViewProperties is ViewProperties viewProperties)
                    {
                        if (viewProperties.view is FrameworkElement control)
                        {
                            if (control.Parent is Panel panel)
                            {
                                panel.Children.Remove(control);
                            }
                            View.Children.Add(control);
                            hasView = true;
                        } else
                        {
                            var plugin = LandingPageExtension.Instance.PlayniteApi.Addons.Plugins.Where(p => p.Id == viewProperties.PluginId).FirstOrDefault();
                            if (plugin is IStartPageExtension extension)
                            {
                                try
                                {
                                    if (LandingPageExtension.Instance.AllAvailableViews.Values.SelectMany(v => v).Where(v => v.PluginId == viewProperties.PluginId && v.ViewId == viewProperties.ViewId).FirstOrDefault() is StartPageViewArgsBase args)
                                    {
                                        viewProperties.StartPageViewArgs = args;
                                        if (extension.GetStartPageView(viewProperties.ViewId, viewProperties.InstanceId) is FrameworkElement control2)
                                        {
                                            control2.Name = viewProperties.ViewId;
                                            viewProperties.view = control2;
                                            View.Children.Add(control2);
                                            hasView = true;
                                        }
                                    }
                                    else
                                    {
                                        gridNode.ViewProperties = null;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LandingPageExtension.logger.Error(ex, $"Failed to load view \"{viewProperties.ViewId}\" from Plugin with ID \"{viewProperties.PluginId}\".");
                                }
                            }
                        }
                    }
                }
                HasView = hasView;
            }
        }

        private void Split(Orientation orientation)
        {
            if (Parent != null)
            {
                if (Parent.GridNode.Children.Count == 0)
                {
                    Parent.GridNode.Orientation = orientation;
                    var temp = GridNode.ViewProperties;
                    GridNode.ViewProperties = null;
                    Parent.GridNode.Children.Add(new GridNode() { ViewProperties = temp, Orientation = GridNode.Orientation, Padding = GridNode.Padding });
                    Parent.GridNode.Children.Add(new GridNode());
                    return;
                }
                else if (Parent.GridNode.Orientation == orientation)
                {
                    var index = Parent.GridNode.Children.IndexOf(GridNode);
                    var length = GridNode.Size.Value / 2;
                    Parent.GridNode.Children[index].Size = new GridLength(length, GridUnitType.Star);
                    Parent.GridNode.Children.Insert(index + 1, new GridNode() { Size = new GridLength(length, GridUnitType.Star) });
                    return;
                }
            }

            if (GridNode.Children.Count == 0)
            {
                GridNode.Orientation = orientation;
                var temp = GridNode.ViewProperties;
                GridNode.ViewProperties = null;
                GridNode.Children.Add(new GridNode() 
                { 
                    ViewProperties = temp, 
                    Orientation = GridNode.Orientation, 
                    Padding = GridNode.Padding, 
                    HorizontalAlignment = GridNode.HorizontalAlignment, 
                    VerticalAlignment = GridNode.VerticalAlignment 
                });
                GridNode.Children.Add(new GridNode());
                return;
            }
            else if (GridNode.Orientation == orientation)
            {
                GridNode.Children.Add(new GridNode());
                return;
            }
        }

        private void CreateView(GridNode node)
        {
            View.Children.Clear();
            View.RowDefinitions.Clear();
            View.ColumnDefinitions.Clear();
            var hasView = false;
            if (node.Children.Count == 0)
            {
                if (gridNode.ViewProperties is ViewProperties viewProperties)
                {
                    if (viewProperties.view is FrameworkElement control)
                    {
                        if (control.Parent is Panel panel)
                        {
                            panel.Children.Remove(control);
                        }
                        View.Children.Add(control);
                        HasView = true;
                        IsLeaf = node.Children.Count == 0;
                    }
                    else
                    {
                        var plugin = LandingPageExtension.Instance.PlayniteApi.Addons.Plugins.Where(p => p.Id == viewProperties.PluginId).FirstOrDefault();
                        if (plugin is IStartPageExtension extension)
                        {
                            try
                            {
                                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    if (LandingPageExtension.Instance.AllAvailableViews.Values.SelectMany(v => v).Where(v => v.PluginId == viewProperties.PluginId && v.ViewId == viewProperties.ViewId).FirstOrDefault() is StartPageViewArgsBase args)
                                    {

                                        viewProperties.StartPageViewArgs = args;
                                        if (extension.GetStartPageView(viewProperties.ViewId, viewProperties.InstanceId) is FrameworkElement control2)
                                        {
                                            control2.Name = viewProperties.ViewId;
                                            viewProperties.view = control2;
                                            View.Children.Add(control2);
                                            hasView = true;
                                            if (control2 is IStartPageControl startPageControl)
                                            {
                                                startPageControl.OnStartPageOpened();
                                            }
                                            if (control2.DataContext is IStartPageControl startPageViewModel)
                                            {
                                                startPageViewModel.OnStartPageOpened();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        gridNode.ViewProperties = null;
                                    }
                                    HasView = hasView;
                                    IsLeaf = node.Children.Count == 0;
                                }), System.Windows.Threading.DispatcherPriority.Normal);
                            }
                            catch (Exception ex)
                            {
                                LandingPageExtension.logger.Error(ex, $"Failed to load view \"{viewProperties.ViewId}\" from Plugin with ID \"{viewProperties.PluginId}\".");
                            }
                        }
                    }
                }
            }
            else
            {
                HasView = node?.ViewProperties?.view != null;
                if (node.Orientation == Orientation.Horizontal)
                {
                    for (int i = 0; i < node.Children.Count; i++)
                    {
                        var column = new ColumnDefinition();
                        column.SetBinding(ColumnDefinition.WidthProperty, new Binding("Size") { Source = node.Children[i], Mode = BindingMode.TwoWay });
                        View.ColumnDefinitions.Add(column);
                        if (i < node.Children.Count - 1)
                        {
                            View.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5) });
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < node.Children.Count; i++)
                    {
                        var row = new RowDefinition();
                        row.SetBinding(RowDefinition.HeightProperty, new Binding("Size") { Source = node.Children[i], Mode = BindingMode.TwoWay });
                        View.RowDefinitions.Add(row);
                        if (i < node.Children.Count - 1)
                        {
                            View.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5) });
                        }
                    }
                }
                if (node.Children.Count > 0)
                {
                    Action<UIElement, int> setGridPosition = Grid.SetRow;
                    if (node.Orientation == Orientation.Horizontal)
                    {
                        setGridPosition = Grid.SetColumn;
                    }
                    for (int i = 0; i < node.Children.Count; ++i)
                    {
                        var child = node.Children[i];
                        var element = new GridNodeView { DataContext = new GridNodeViewModel(node.Children[i]) { Parent = this, EditModeEnabled = EditModeEnabled } };
                        setGridPosition(element, View.Children.Count);
                        View.Children.Add(element);
                        if (i < node.Children.Count - 1)
                        {
                            var curr = i;
                            var sep = new GridSplitter() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
                            WindowChrome.SetIsHitTestVisibleInChrome(sep, true);

                            var mergeItem = GetMergeMenuItem(node, curr);
                            var addItem = GetAddMenuItem(node);
                            sep.ContextMenu = new ContextMenu() { DataContext = this };
                            sep.ContextMenu.Items.Add(mergeItem);
                            sep.ContextMenu.Items.Add(addItem);

                            setGridPosition(sep, View.Children.Count);
                            View.Children.Add(sep);
                        }
                    }
                }
            }
            IsLeaf = node.Children.Count == 0;
        }

        private static MenuItem GetAddMenuItem(GridNode node)
        {
            MenuItem addItem = new MenuItem
            {
                Header = ResourceProvider.GetString("LOC_SPG_AddPanel")
            };
            addItem.Items.Add(new MenuItem
            {
                Header = node.Orientation == Orientation.Horizontal ? ResourceProvider.GetString("LOC_SPG_AddPanelLeft") : ResourceProvider.GetString("LOC_SPG_AddPanelTop"),
                Command = new RelayCommand(() =>
                {
                    var newSize = node.Children.First().Size.Value / 2;
                    var newNode = new GridNode { Size = new GridLength(newSize, GridUnitType.Star) };
                    node.Children.Insert(0, newNode);
                })
            });
            addItem.Items.Add(new MenuItem
            {
                Header = node.Orientation == Orientation.Horizontal ? ResourceProvider.GetString("LOC_SPG_AddPanelRight") : ResourceProvider.GetString("LOC_SPG_AddPanelBottom"),
                Command = new RelayCommand(() =>
                {
                    var newSize = node.Children.Last().Size.Value / 2;
                    var newNode = new GridNode { Size = new GridLength(newSize, GridUnitType.Star) };
                    node.Children.Add(newNode);
                })
            });
            return addItem;
        }

        private MenuItem GetMergeMenuItem(GridNode node, int curr)
        {
            MenuItem mergeItem = new MenuItem
            {
                Header = ResourceProvider.GetString("LOC_SPG_Merge")
            };
            mergeItem.Items.Add(new MenuItem
            {
                Header = node.Orientation == Orientation.Horizontal ? ResourceProvider.GetString("LOC_SPG_MergeKeepLeft") : ResourceProvider.GetString("LOC_SPG_MergeKeepUpper"),
                Command = new RelayCommand(() =>
                {
                    var root = Root.GridNode;
                    var left = node.Children[curr];
                    var right = node.Children[curr + 1];
                    var newSize = left.Size.Value + right.Size.Value;
                    node.Children[curr].Size = new GridLength(newSize, GridUnitType.Star);
                    node.Children.RemoveAt(curr + 1);
                    GridNode.Minimize(root, null);
                })
            });
            mergeItem.Items.Add(new MenuItem
            {
                Header = node.Orientation == Orientation.Horizontal ? ResourceProvider.GetString("LOC_SPG_MergeKeepRight") : ResourceProvider.GetString("LOC_SPG_MergeKeepLower"),
                Command = new RelayCommand(() =>
                {
                    var root = Root.GridNode;
                    var left = node.Children[curr];
                    var right = node.Children[curr + 1];
                    var newSize = left.Size.Value + right.Size.Value;
                    node.Children[curr + 1].Size = new GridLength(newSize, GridUnitType.Star);
                    node.Children.RemoveAt(curr);
                    GridNode.Minimize(root, null);
                })
            });
            return mergeItem;
        }

        private void Children_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach(GridNode removed in e.OldItems)
                {
                    var model = View.Children.OfType<FrameworkElement>().FirstOrDefault(fe =>
                    {
                        if (fe.DataContext is GridNodeViewModel m)
                        {
                            return m.GridNode == removed;
                        }
                        return false;
                    })?.DataContext as GridNodeViewModel;
                    if (model != null)
                    {
                        for (int i = model.GridNode.Children.Count - 1; i >= 0; i--)
                        {
                            model.GridNode.Children.RemoveAt(i);
                        }
                        OnNodeRemoved(removed);
                        if (model.GridNode.ViewProperties != null)
                        {
                            var props = model.GridNode.ViewProperties;
                            model.GridNode.ViewProperties = null;
                        }
                    }
                }
            }
            CreateView(GridNode);
        }
    }
}
