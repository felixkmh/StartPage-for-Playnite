using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LandingPage.Models.Objects
{
    public class ViewModelCollection<TSource, TModel> : ObservableCollection<TModel>, IDisposable
    {
        private ICollection<TSource> _sourceCollection;
        private readonly Func<TSource, TModel> _factory;
        private readonly Dispatcher _dispatcher;

        public ViewModelCollection(ICollection<TSource> sourceCollection, Func<TSource, TModel> factory, Dispatcher dispatcher) : base(sourceCollection.Select(item => factory(item)))
        {
            _sourceCollection = sourceCollection;
            _factory = factory;
            _dispatcher = dispatcher;

            if (sourceCollection is INotifyCollectionChanged ncc)
            {
                ncc.CollectionChanged += SourceCollectionChanged;
            }
        }

        private void SourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _dispatcher.Invoke(() =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    ClearItems();
                }
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    for(int i = 0; i < e.NewItems.Count; i++)
                    {
                        InsertItem(e.NewStartingIndex + i, _factory((TSource)e.NewItems[i]));
                    }
                }
                if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        RemoveItem(e.OldStartingIndex);
                    }
                }
                if (e.Action == NotifyCollectionChangedAction.Replace)
                {
                    for(int i = 0; i < e.OldItems.Count; i++)
                    {
                        this[e.OldStartingIndex + i] = _factory((TSource)e.NewItems[i]);
                    }
                }
                if (e.Action == NotifyCollectionChangedAction.Move)
                {
                    (this[e.NewStartingIndex], this[e.OldStartingIndex]) = (this[e.OldStartingIndex], this[e.NewStartingIndex]);
                }
            });
        }

        public void Dispose()
        {
            if (_sourceCollection is INotifyCollectionChanged ncc)
            {
                ncc.CollectionChanged -= SourceCollectionChanged;
            }
        }
    }
}
