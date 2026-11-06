// Port of the exception hooks in App.xaml.cs. AppDomain.UnhandledException and TaskScheduler.UnobservedTaskException
// are identical. WPF's DispatcherUnhandledException becomes Avalonia's Dispatcher.UIThread.UnhandledException with the
// same behavior (log fatal, flush, leave unhandled so the process ends). Dropped, because they cannot occur: the COMException
// -2147221040 clipboard workaround and the WPF LiveCharts CompositionTargetTicker disposal NullReferenceException
// (the Avalonia LiveCharts package has no such ticker).
using Avalonia.Threading;
using Serilog;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace StatisticsAnalysisTool.Linux.Startup;

public static class GlobalExceptionHandlers
{
    public static void Install()
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        Dispatcher.UIThread.UnhandledException += UiThread_UnhandledException;
    }

    public static void Uninstall()
    {
        AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;
        Dispatcher.UIThread.UnhandledException -= UiThread_UnhandledException;
    }

    internal static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception exception)
            {
                Log.Fatal(exception, "Unhandled exception in {Source}", MethodBase.GetCurrentMethod()?.DeclaringType);
            }
            else
            {
                Log.Fatal("Unhandled exception object: {ExceptionObject} in {Source}", e.ExceptionObject, MethodBase.GetCurrentMethod()?.DeclaringType);
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Error during unhandled exception logging in {Source}", MethodBase.GetCurrentMethod()?.DeclaringType);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    internal static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Unhandled exception in TaskScheduler");
        e.SetObserved();
    }

    internal static void UiThread_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            Log.Fatal(e.Exception, "Unhandled exception in UI thread");
        }
        finally
        {
            Log.CloseAndFlush();
            e.Handled = false;
        }
    }
}
