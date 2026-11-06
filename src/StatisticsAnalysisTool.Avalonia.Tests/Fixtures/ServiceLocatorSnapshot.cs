using System.Collections;
using System.Reflection;
using StatisticsAnalysisTool.Common;

namespace StatisticsAnalysisTool.Avalonia.Tests;

/// <summary>
/// ServiceLocator is a process-wide static dictionary. A test that runs the real app startup registers a TrackingController,
/// view models and services that outlive it; later tests then see (and talk to) a half-disposed app. The snapshot puts the
/// dictionary back exactly as it was.
/// </summary>
internal sealed class ServiceLocatorSnapshot : IDisposable
{
    private static readonly FieldInfo ServicesField = typeof(ServiceLocator).GetField("Services", BindingFlags.Static | BindingFlags.NonPublic)!;

    private readonly Dictionary<Type, object> _saved = new();

    public ServiceLocatorSnapshot()
    {
        foreach (DictionaryEntry entry in Services)
        {
            _saved[(Type) entry.Key] = entry.Value;
        }
    }

    public static void Remove<T>() => Services.Remove(typeof(T));

    public void Dispose()
    {
        Services.Clear();
        foreach (var (type, service) in _saved)
        {
            Services[type] = service;
        }
    }

    private static IDictionary Services => (IDictionary) ServicesField.GetValue(null)!;
}
