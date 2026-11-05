using Avalonia.Headless.NUnit;
using FluentAssertions;
using StatisticsAnalysisTool.ViewModels;
using StatisticsAnalysisTool.Views;

namespace StatisticsAnalysisTool.Avalonia.Tests.Startup;

public class ToolLoadingWindowTests
{
    [AvaloniaTest]
    public void Window_ShowsTheViewModelProgress()
    {
        var viewModel = new ToolLoadingWindowViewModel();
        var window = new ToolLoadingWindow(viewModel);

        window.Show();
        viewModel.UpdateProgress(40, "Backups");

        window.DataContext.Should().BeSameAs(viewModel);
        viewModel.ProgressBarValue.Should().Be(40);
        viewModel.LoadingText.Should().Contain("Backups").And.Contain("40%");
        window.Close();
    }
}
