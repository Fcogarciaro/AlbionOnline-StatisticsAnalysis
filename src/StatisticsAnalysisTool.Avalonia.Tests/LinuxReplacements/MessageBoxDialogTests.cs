using System.Windows;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using FluentAssertions;
using AvaloniaWindow = Avalonia.Controls.Window;

namespace StatisticsAnalysisTool.Avalonia.Tests.LinuxReplacements;

public class MessageBoxDialogTests
{
    private static MessageBoxResult ShowAndClick(MessageBoxButton buttons, string buttonLabel)
    {
        var owner = new AvaloniaWindow();
        owner.Show();
        var dialog = new MessageBoxDialog("Delete it?", "Confirm", buttons, MessageBoxImage.Question);
        dialog.Opened += (_, _) =>
            dialog.GetVisualDescendants().OfType<Button>().Single(b => (string) b.Content == buttonLabel)
                .RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

        dialog.ShowDialogBlocking(owner);
        return dialog.Result;
    }

    [AvaloniaTest]
    public void ClickingYes_ReturnsYes() => ShowAndClick(MessageBoxButton.YesNoCancel, "Yes").Should().Be(MessageBoxResult.Yes);

    [AvaloniaTest]
    public void ClickingNo_ReturnsNo() => ShowAndClick(MessageBoxButton.YesNo, "No").Should().Be(MessageBoxResult.No);

    [AvaloniaTest]
    public void ClosingWithoutAButton_ReturnsTheCancelLikeResult()
    {
        var owner = new AvaloniaWindow();
        owner.Show();
        var dialog = new MessageBoxDialog("x", "y", MessageBoxButton.OKCancel, MessageBoxImage.None);
        dialog.Opened += (_, _) => dialog.Close();

        dialog.ShowDialogBlocking(owner);

        dialog.Result.Should().Be(MessageBoxResult.Cancel);
    }
}
