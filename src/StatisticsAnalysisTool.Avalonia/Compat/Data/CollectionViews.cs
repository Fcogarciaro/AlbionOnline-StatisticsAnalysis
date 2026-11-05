// Compatibility shim: System.Windows.Data.ListCollectionView / CollectionViewSource and the
// System.ComponentModel view contracts (ICollectionView, SortDescription) used by the linked ViewModels.
// The view is a materialized, filtered and sorted projection of the source that follows
// INotifyCollectionChanged sources and is rebuilt on Refresh (like WPF for plain lists).
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.ComponentModel
{
    public readonly struct SortDescription(string propertyName, ListSortDirection direction)
    {
        public string PropertyName { get; } = propertyName;

        public ListSortDirection Direction { get; } = direction;
    }

    public class SortDescriptionCollection : ObservableCollection<SortDescription>
    {
    }

    public interface ICollectionView : IEnumerable, INotifyCollectionChanged
    {
        IEnumerable SourceCollection { get; }

        Predicate<object> Filter { get; set; }

        bool CanFilter { get; }

        SortDescriptionCollection SortDescriptions { get; }

        bool CanSort { get; }

        bool Contains(object item);

        void Refresh();

        IDisposable DeferRefresh();
    }
}

namespace System.Windows.Data
{
    public class ListCollectionView : ICollectionView, INotifyPropertyChanged
    {
        private readonly IEnumerable _source;
        private readonly List<object> _items = [];
        private Predicate<object> _filter;
        private int _deferLevel;
        private bool _refreshPending;

        public ListCollectionView(IList list) : this((IEnumerable) list)
        {
        }

        internal ListCollectionView(IEnumerable source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            SortDescriptions = new SortDescriptionCollection();
            SortDescriptions.CollectionChanged += (_, _) => RefreshOrDefer();

            if (_source is INotifyCollectionChanged notifying)
            {
                notifying.CollectionChanged += OnSourceCollectionChanged;
            }

            Rebuild();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public IEnumerable SourceCollection => _source;

        public Predicate<object> Filter
        {
            get => _filter;
            set
            {
                _filter = value;
                RefreshOrDefer();
            }
        }

        public bool CanFilter => true;

        public bool CanSort => true;

        public SortDescriptionCollection SortDescriptions { get; }

        public int Count => _items.Count;

        public object GetItemAt(int index) => _items[index];

        public bool Contains(object item) => _items.Contains(item);

        public IEnumerator GetEnumerator() => _items.ToList().GetEnumerator();

        public void Refresh()
        {
            Rebuild();
            RaiseReset();
        }

        public IDisposable DeferRefresh()
        {
            _deferLevel++;
            return new DeferScope(this);
        }

        private void RefreshOrDefer()
        {
            if (_deferLevel > 0)
            {
                _refreshPending = true;
                return;
            }

            Refresh();
        }

        private void EndDefer()
        {
            if (--_deferLevel == 0 && _refreshPending)
            {
                _refreshPending = false;
                Refresh();
            }
        }

        private void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_deferLevel > 0)
            {
                _refreshPending = true;
                return;
            }

            // Fast path: unsorted append at the end of the source keeps order without a full rebuild.
            if (e.Action == NotifyCollectionChangedAction.Add && SortDescriptions.Count == 0 && e.NewItems != null
                && e.NewStartingIndex >= 0 && e.NewStartingIndex + e.NewItems.Count == CountSource())
            {
                var added = e.NewItems.Cast<object>().Where(PassesFilter).ToList();
                if (added.Count == 0)
                {
                    return;
                }

                _items.AddRange(added);
                RaiseReset();
                return;
            }

            Refresh();
        }

        private int CountSource() => _source is ICollection collection ? collection.Count : _source.Cast<object>().Count();

        private bool PassesFilter(object item) => _filter == null || _filter(item);

        private void Rebuild()
        {
            IEnumerable<object> query = _source.Cast<object>().ToList().Where(PassesFilter);

            IOrderedEnumerable<object> ordered = null;
            foreach (var description in SortDescriptions)
            {
                var name = description.PropertyName;
                var descending = description.Direction == ListSortDirection.Descending;
                ordered = ordered switch
                {
                    null when descending => query.OrderByDescending(i => GetValue(i, name), Comparer<object>.Create(CompareValues)),
                    null => query.OrderBy(i => GetValue(i, name), Comparer<object>.Create(CompareValues)),
                    _ when descending => ordered.ThenByDescending(i => GetValue(i, name), Comparer<object>.Create(CompareValues)),
                    _ => ordered.ThenBy(i => GetValue(i, name), Comparer<object>.Create(CompareValues))
                };
            }

            _items.Clear();
            _items.AddRange(ordered ?? query);
        }

        private static object GetValue(object item, string path)
        {
            object current = item;
            foreach (var segment in path.Split('.'))
            {
                if (current == null)
                {
                    return null;
                }

                current = current.GetType().GetProperty(segment, BindingFlags.Public | BindingFlags.Instance)?.GetValue(current);
            }

            return current;
        }

        private static int CompareValues(object x, object y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return x is string sx && y is string sy
                ? string.Compare(sx, sy, Globalization.CultureInfo.CurrentCulture, Globalization.CompareOptions.None)
                : Comparer<object>.Default.Compare(x, y);
        }

        private void RaiseReset()
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        }

        private sealed class DeferScope(ListCollectionView owner) : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                owner.EndDefer();
            }
        }
    }

    public static class CollectionViewSource
    {
        private static readonly ConditionalWeakTable<object, ICollectionView> Views = new();

        public static ICollectionView GetDefaultView(object source)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (source is ICollectionView existing)
            {
                return existing;
            }

            return Views.GetValue(source, s => new ListCollectionView((IEnumerable) s));
        }
    }
}
