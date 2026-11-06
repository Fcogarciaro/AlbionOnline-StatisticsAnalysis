using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace StatisticsAnalysisTool.Avalonia.Tests.Compat;

[NonParallelizable]
public class ClipboardTests
{
    [AvaloniaTest]
    public void SetText_WritesToTheClipboardOfAnyVisibleWindow_WhenThereIsNoMainWindow()
    {
        var window = new Window { Width = 100, Height = 100 };
        window.Show();
        var hidden = new Window();

        var picked = System.Windows.Clipboard.PickWindow(null, [hidden, window]);

        picked.Should().BeSameAs(window, "the main window is hidden to the tray or not created yet, a visible one still owns a clipboard");
        System.Windows.Clipboard.PickWindow(hidden, [window]).Should().BeSameAs(hidden, "the main window wins even while hidden");
        System.Windows.Clipboard.PickWindow(null, [hidden]).Should().BeNull();
        window.Close();
    }

    [AvaloniaTest]
    public async Task SetText_ReachesTheWindowClipboard()
    {
        var window = new Window { Width = 100, Height = 100 };
        window.Show();
        System.Windows.Clipboard.ResolveOverride = () => window;
        try
        {
            System.Windows.Clipboard.SetText("hello from sat");
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(50);
            Dispatcher.UIThread.RunJobs();

            var text = await window.Clipboard!.TryGetTextAsync();
            text.Should().Be("hello from sat");
        }
        finally
        {
            System.Windows.Clipboard.ResolveOverride = null;
            window.Close();
        }
    }
}
