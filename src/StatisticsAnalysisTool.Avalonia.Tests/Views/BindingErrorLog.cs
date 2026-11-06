using Avalonia.Logging;

namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

/// <summary>Collects Avalonia binding/property/styling warnings while a view is created and shown.</summary>
internal sealed class BindingErrorLog : ILogSink, IDisposable
{
    private readonly ILogSink _previous = Logger.Sink;

    public BindingErrorLog() => Logger.Sink = this;

    public List<string> Errors { get; } = [];

    public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning && area is "Binding" or "Property" or "Styling" or "Control";

    public void Log(LogEventLevel level, string area, object source, string messageTemplate) => Errors.Add($"{area}: {messageTemplate}");

    public void Log(LogEventLevel level, string area, object source, string messageTemplate, params object[] propertyValues)
        => Errors.Add($"{area}: {messageTemplate} [{string.Join(", ", propertyValues)}]");

    public void Dispose() => Logger.Sink = _previous;
}
