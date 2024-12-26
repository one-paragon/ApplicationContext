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