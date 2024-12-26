using System.Collections.Immutable;

namespace OneParagon.Infrastructure;

public static class ApplicationContext
{
    static ApplicationContext()
    {
        _features = new();
    }

    public static ImmutableDictionary<Type, object> DefaultFeatures { get; set; } = ImmutableDictionary<Type, object>.Empty;

    private static ImmutableDictionary<Type, object> Features => _features.Value is null ? DefaultFeatures : _features.Value;

    private static AsyncLocal<ImmutableDictionary<Type, object>> _features;

    public static void SetKeyedFeature<T>(string key, T value)
    {
        var dict = GetFeature<ImmutableDictionary<string, T>>() ?? ImmutableDictionary<string, T>.Empty;
        dict = dict.SetItem(key, value);
        SetFeature(dict);
    }

    public static T? GetKeyedFeature<T>(string key)
    {
        var dict = GetFeature<ImmutableDictionary<string, T>>();
        if (dict == null) return default;
        if (dict.TryGetValue(key, out var value)) return value;
        return default;
    }

    public static ImmutableDictionary<Type, object> CaptureContext() => Features;

    public static void ResetContext(ImmutableDictionary<Type, object> features) => _features.Value = features;

    public static Func<Task> WrapInContext(Func<Task> func)
    {
        var capturedContext = CaptureContext();
        return async () =>
        {
            ResetContext(capturedContext);
            await func();
        };
    }

    public static Func<Task<T1>> WrapInContext<T1>(Func<Task<T1>> func)
    {
        var capturedContext = CaptureContext();
        return async () =>
        {
            ResetContext(capturedContext);
            return await func();
        };
    }

    public static Func<T1, Task<T2>> WrapInContext<T1, T2>(Func<T1, Task<T2>> func)
    {
        var capturedContext = CaptureContext();
        return async (T1 t) =>
        {
            ResetContext(capturedContext);
            return await func(t);
        };
    }

    public static Func<T, Task> WrapInContext<T>(Func<T, Task> func)
    {
        var capturedContext = CaptureContext();
        return async (T t) =>
        {
            ResetContext(capturedContext);
            await func(t);
        };
    }

    public static void SetFeature<T>(T obj)
        where T : notnull
    {
        _features.Value = Features.SetItem(typeof(T), obj);
    }

    public static void SetFeatures(ImmutableDictionary<Type, object> features)
    {
        _features.Value = Features.SetItems(features);
    }

    public static T? GetFeature<T>()
    {
        if (Features.TryGetValue(typeof(T), out var value))
        {
            return (T)value;
        }
        else
        {
            return default(T);
        }
    }

    public static T GetRequiredFeature<T>()
    {
        if (Features.TryGetValue(typeof(T), out var value))
        {
            return (T)value;
        }
        else
        {
            throw new InvalidOperationException("Feature " + typeof(T) + " has not been added to the current context.");
        }
    }
}