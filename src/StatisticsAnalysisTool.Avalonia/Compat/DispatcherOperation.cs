// Compatibility shim: mirrors System.Windows.Threading.DispatcherOperation / DispatcherOperation<T>
// on top of Avalonia's dispatcher operations.
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AvaloniaOperation = Avalonia.Threading.DispatcherOperation;

namespace System.Windows.Threading;

public enum DispatcherOperationStatus
{
    Pending = 0,
    Aborted = 1,
    Completed = 2,
    Executing = 3
}

public class DispatcherOperation
{
    private readonly AvaloniaOperation _operation;

    internal DispatcherOperation(AvaloniaOperation operation)
    {
        _operation = operation;
    }

    public Task Task => _operation.GetTask();

    public DispatcherOperationStatus Status => _operation.Status switch
    {
        Avalonia.Threading.DispatcherOperationStatus.Pending => DispatcherOperationStatus.Pending,
        Avalonia.Threading.DispatcherOperationStatus.Executing => DispatcherOperationStatus.Executing,
        Avalonia.Threading.DispatcherOperationStatus.Completed => DispatcherOperationStatus.Completed,
        _ => DispatcherOperationStatus.Aborted
    };

    public bool Abort() => _operation.Abort();

    public void Wait() => _operation.Wait();

    public TaskAwaiter GetAwaiter() => Task.GetAwaiter();
}

public sealed class DispatcherOperation<T> : DispatcherOperation
{
    private readonly Avalonia.Threading.DispatcherOperation<T> _typed;

    internal DispatcherOperation(Avalonia.Threading.DispatcherOperation<T> operation) : base(operation)
    {
        _typed = operation;
    }

    public new Task<T> Task => _typed.GetTask();

    public T Result => _typed.Result;

    public new TaskAwaiter<T> GetAwaiter() => Task.GetAwaiter();
}
