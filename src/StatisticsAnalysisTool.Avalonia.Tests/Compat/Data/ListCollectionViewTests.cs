using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using FluentAssertions;
using NUnit.Framework;

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
}
