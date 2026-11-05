using System.Windows;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using AvaloniaDispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Compat;

public class WindowShimTests
{
    private sealed class AcceptingDialog : Window
    {
        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            DialogResult = true;
        }
    }

    [AvaloniaTest]
    public void ShowDialogBlocking_ReturnsDialogResultSetByTheDialog()
    {
        var owner = new global::Avalonia.Controls.Window();
        owner.Show();
        var dialog = new AcceptingDialog();

        var result = dialog.ShowDialogBlocking(owner);

        result.Should().BeTrue();
    }

    [AvaloniaTest]
    public void ShowDialogBlocking_ClosedWithoutResult_ReturnsFalse()
    {
        var owner = new global::Avalonia.Controls.Window();
        owner.Show();
        var dialog = new Window();
        dialog.Opened += (_, _) => dialog.Close();

        dialog.ShowDialogBlocking(owner).Should().BeFalse();
    }

    [AvaloniaTest]
    public void Dispatcher_ExposesTheCompatDispatcher()
    {
        new Window().Dispatcher.Should().BeSameAs(System.Windows.Threading.Dispatcher.CurrentDispatcher);
    }

    [AvaloniaTest]
    public void LeftAndTop_MapToWindowPosition()
    {
        var window = new Window { Left = 15, Top = 25 };

        window.Position.X.Should().Be(15);
        window.Position.Y.Should().Be(25);
        window.Left.Should().Be(15);
    }
}
