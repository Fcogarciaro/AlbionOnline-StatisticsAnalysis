using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using NUnit.Framework;
using AvaloniaDispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Compat.Data;

public class ListCollectionViewTests
{
    private sealed record Row(string Name, int Value);

    private static ObservableCollection<Row> Source() =>
        new() { new Row("c", 3), new Row("a", 1), new Row("b", 2) };

    [Test]
    public void New_ExposesAllItemsInSourceOrder()
    {
        var view = new ListCollectionView(Source());

        view.Cast<Row>().Select(r => r.Name).Should().Equal("c", "a", "b");
        view.Count.Should().Be(3);
    }

    [Test]
    public void Filter_Set_AppliesImmediatelyAndCountReflectsFilteredItems()
    {
        var view = new ListCollectionView(Source());

        view.Filter = o => ((Row) o).Value >= 2;

        view.Cast<Row>().Select(r => r.Name).Should().Equal("c", "b");
        view.Count.Should().Be(2);
    }

    [Test]
    public void Filter_SetToNull_RestoresAllItems()
    {
        var view = new ListCollectionView(Source()) { Filter = o => false };

        view.Filter = null;

        view.Count.Should().Be(3);
    }

    [Test]
    public void SortDescriptions_Added_SortAscendingAndDescendingStably()
    {
        var source = new ObservableCollection<Row> { new("x", 1), new("y", 2), new("z", 1) };
        var view = new ListCollectionView(source);

        view.SortDescriptions.Add(new SortDescription(nameof(Row.Value), ListSortDirection.Descending));

        view.Cast<Row>().Select(r => r.Name).Should().Equal("y", "x", "z");
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(nameof(Row.Name), ListSortDirection.Ascending));
        view.Cast<Row>().Select(r => r.Name).Should().Equal("x", "y", "z");
    }

    [Test]
    public void Refresh_ReevaluatesFilterAgainstMutatedItems()
    {
        var items = new List<Row> { new("a", 1), new("b", 2) };
        var threshold = 0;
        var view = new ListCollectionView(items) { Filter = o => ((Row) o).Value > threshold };
        view.Count.Should().Be(2);

        threshold = 1;
        view.Count.Should().Be(2, "a plain list gives no notifications until Refresh is called");
        view.Refresh();

        view.Count.Should().Be(1);
    }

    [Test]
    public void SourceCollectionChanged_AddedItemPassingFilterAppears_AndRaisesCollectionChanged()
    {
        var source = Source();
        var view = new ListCollectionView(source) { Filter = o => ((Row) o).Value > 1 };
        var raised = 0;
        ((INotifyCollectionChanged) view).CollectionChanged += (_, _) => raised++;

        source.Add(new Row("d", 4));
        source.Add(new Row("e", 0));

        view.Cast<Row>().Select(r => r.Name).Should().Equal("c", "b", "d");
        raised.Should().BeGreaterThan(0);
    }

    [Test]
    public void SourceCollectionChanged_RemoveAndClearAreReflected()
    {
        var source = Source();
        var view = new ListCollectionView(source);

        source.RemoveAt(0);
        view.Count.Should().Be(2);
        source.Clear();
        view.Count.Should().Be(0);
    }

    [Test]
    public void SourceCollectionChanged_WithSort_KeepsViewSorted()
    {
        var source = Source();
        var view = new ListCollectionView(source);
        view.SortDescriptions.Add(new SortDescription(nameof(Row.Value), ListSortDirection.Ascending));

        source.Add(new Row("zero", 0));

        view.Cast<Row>().First().Name.Should().Be("zero");
    }

    [Test]
    public void DeferRefresh_SuppressesRefreshUntilDisposed()
    {
        var view = new ListCollectionView(Source());

        using (view.DeferRefresh())
        {
            view.Filter = o => false;
            view.Count.Should().Be(3);
        }

        view.Count.Should().Be(0);
    }

    [Test]
    public void GetDefaultView_ReturnsSameListCollectionViewPerSource()
    {
        var source = Source();

        var first = CollectionViewSource.GetDefaultView(source);
        var second = CollectionViewSource.GetDefaultView(source);

        first.Should().BeOfType<ListCollectionView>();
        second.Should().BeSameAs(first);
        CollectionViewSource.GetDefaultView(Source()).Should().NotBeSameAs(first);
    }

    [Test]
    public void ICollectionView_ExposesFilterAndRefresh()
    {
        ICollectionView view = new ListCollectionView(Source());

        view.Filter = o => ((Row) o).Name == "a";
        view.Refresh();

        view.Cast<Row>().Should().ContainSingle();
        view.CanFilter.Should().BeTrue();
    }

    [AvaloniaTest]
    public void View_IsAReadOnlyIListSoAvaloniaItemsControlsCanBindIt()
    {
        var source = new ObservableCollection<int> { 3, 1, 2 };
        var view = new ListCollectionView(source);
        view.SortDescriptions.Add(new SortDescription(string.Empty, ListSortDirection.Ascending));
        var list = (System.Collections.IList) view;

        list.Count.Should().Be(3);
        list.IsReadOnly.Should().BeTrue();
        list.IsFixedSize.Should().BeFalse();
        list[0].Should().NotBeNull();
        list.Contains(list[1]).Should().BeTrue();
        list.IndexOf(list[2]).Should().Be(2);
        var copy = new object[3];
        list.CopyTo(copy, 0);
        copy.Should().Equal(list.Cast<object>());
        ((Action) (() => list.Add(1))).Should().Throw<NotSupportedException>();
        ((Action) (() => list.Remove(1))).Should().Throw<NotSupportedException>();
        ((Action) (() => list[0] = 1)).Should().Throw<NotSupportedException>();
    }

    [AvaloniaTest]
    public void View_CanBeTheItemsSourceOfAnItemsControl()
    {
        var source = new ObservableCollection<string> { "a", "b" };
        var control = new global::Avalonia.Controls.ItemsControl { ItemsSource = new ListCollectionView(source) };
        var window = new global::Avalonia.Controls.Window { Content = control };
        window.Show();

        control.ItemCount.Should().Be(2);
        source.Add("c");
        AvaloniaDispatcher.UIThread.RunJobs();
        control.ItemCount.Should().Be(3);
        window.Close();
    }

    private sealed class Observable : INotifyPropertyChanged
    {
        private int _value;

        public string Name { get; init; }

        public int Value
        {
            get => _value;
            set
            {
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    private sealed class ByValueDescending : System.Collections.IComparer
    {
        public int Compare(object x, object y) => ((Observable) y).Value.CompareTo(((Observable) x).Value);
    }

    [Test]
    public void CustomSort_OrdersItemsWithComparer_AndOverridesSortDescriptions()
    {
        var source = new ObservableCollection<Observable> { new() { Name = "a", Value = 1 }, new() { Name = "b", Value = 3 }, new() { Name = "c", Value = 2 } };
        var view = new ListCollectionView(source);

        view.CustomSort = new ByValueDescending();

        view.Cast<Observable>().Select(o => o.Name).Should().Equal("b", "c", "a");
        view.CustomSort.Should().BeOfType<ByValueDescending>();
    }

    [AvaloniaTest]
    public void LiveSorting_ResortsWhenObservedPropertyChanges()
    {
        var a = new Observable { Name = "a", Value = 1 };
        var b = new Observable { Name = "b", Value = 2 };
        var source = new ObservableCollection<Observable> { a, b };
        var view = new ListCollectionView(source) { IsLiveSorting = true, CustomSort = new ByValueDescending() };
        view.LiveSortingProperties.Add(nameof(Observable.Value));
        view.Cast<Observable>().Select(o => o.Name).Should().Equal("b", "a");

        a.Value = 10;
        AvaloniaDispatcher.UIThread.RunJobs();

        view.Cast<Observable>().Select(o => o.Name).Should().Equal("a", "b");
    }

    [AvaloniaTest]
    public void LiveFiltering_ReevaluatesFilterWhenObservedPropertyChanges()
    {
        var a = new Observable { Name = "a", Value = 1 };
        var source = new ObservableCollection<Observable> { a };
        var view = new ListCollectionView(source) { IsLiveFiltering = true, Filter = o => ((Observable) o).Value > 5 };
        view.LiveFilteringProperties.Add(nameof(Observable.Value));
        view.Count.Should().Be(0);

        a.Value = 9;
        AvaloniaDispatcher.UIThread.RunJobs();

        view.Count.Should().Be(1);
    }

    [AvaloniaTest]
    public void LiveShaping_Disabled_IgnoresPropertyChanges()
    {
        var a = new Observable { Name = "a", Value = 1 };
        var view = new ListCollectionView(new ObservableCollection<Observable> { a }) { Filter = o => ((Observable) o).Value > 5 };

        a.Value = 9;
        AvaloniaDispatcher.UIThread.RunJobs();

        view.Count.Should().Be(0);
    }

    [Test]
    public void Currency_StartsAtFirstItem_AndCurrentChangedFiresOnMove()
    {
        var view = new ListCollectionView(Source());
        var changed = 0;
        view.CurrentChanged += (_, _) => changed++;
        view.CurrentPosition.Should().Be(0);

        view.MoveCurrentToLast();

        ((Row) view.CurrentItem).Name.Should().Be("b");
        changed.Should().Be(1);
    }

    [Test]
    public void Refresh_KeepsCurrentItemWhenStillPresent_AndMovesToFirstWhenRemoved()
    {
        var source = Source();
        var view = new ListCollectionView(source);
        view.MoveCurrentToLast();
        var changed = 0;
        view.CurrentChanged += (_, _) => changed++;

        view.Refresh();
        changed.Should().Be(0);
        source.RemoveAt(2);

        ((Row) view.CurrentItem).Name.Should().Be("c");
        changed.Should().Be(1);
    }

    private static List<NotifyCollectionChangedEventArgs> Capture(ListCollectionView view)
    {
        var events = new List<NotifyCollectionChangedEventArgs>();
        ((INotifyCollectionChanged) view).CollectionChanged += (_, e) => events.Add(e);
        return events;
    }

    [Test]
    public void Unshaped_InsertInMiddle_RaisesIncrementalAddWithIndex()
    {
        var source = Source();
        var view = new ListCollectionView(source);
        var events = Capture(view);

        source.Insert(1, new Row("n", 9));

        events.Should().ContainSingle();
        events[0].Action.Should().Be(NotifyCollectionChangedAction.Add);
        events[0].NewStartingIndex.Should().Be(1);
        view.Cast<Row>().Select(r => r.Name).Should().Equal("c", "n", "a", "b");
    }

    [Test]
    public void Unshaped_Remove_RaisesIncrementalRemoveWithIndex()
    {
        var source = Source();
        var view = new ListCollectionView(source);
        var events = Capture(view);

        source.RemoveAt(1);

        events.Should().ContainSingle();
        events[0].Action.Should().Be(NotifyCollectionChangedAction.Remove);
        events[0].OldStartingIndex.Should().Be(1);
        view.Cast<Row>().Select(r => r.Name).Should().Equal("c", "b");
    }

    [Test]
    public void Unshaped_ReplaceAndMove_RaiseIncrementalEvents()
    {
        var source = Source();
        var view = new ListCollectionView(source);
        var events = Capture(view);

        source[0] = new Row("z", 0);
        source.Move(0, 2);

        events.Select(e => e.Action).Should().Equal(NotifyCollectionChangedAction.Replace, NotifyCollectionChangedAction.Move);
        events[1].OldStartingIndex.Should().Be(0);
        events[1].NewStartingIndex.Should().Be(2);
        view.Cast<Row>().Select(r => r.Name).Should().Equal("a", "b", "z");
    }

    [Test]
    public void ActiveFilter_KeepsResetSemantics()
    {
        var source = Source();
        var view = new ListCollectionView(source) { Filter = o => ((Row) o).Value > 0 };
        var events = Capture(view);

        source.Insert(1, new Row("n", 9));

        events.Should().OnlyContain(e => e.Action == NotifyCollectionChangedAction.Reset);
    }

    [Test]
    public void Dispose_DetachesFromTheSource()
    {
        var source = Source();
        var view = new ListCollectionView(source);
        var events = Capture(view);

        view.Dispose();
        source.Add(new Row("q", 4));

        events.Should().BeEmpty();
    }
}
