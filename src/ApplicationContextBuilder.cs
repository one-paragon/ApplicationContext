using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace OneParagon.Infrasctucture;

public class ApplicationContextBuilder
{
    static T ExtractType<T>(IEnumerable<object> objects)
        where T : notnull
    {
        var t = objects.OfType<T>().FirstOrDefault();
        if (t is null)
        {
            t = objects.OfType<IServiceProvider>().First().GetRequiredService<T>();
        }
        return t;
    }

    abstract class InternalBuilder
    {
        public abstract IAsyncEnumerable<KeyValuePair<Type, object>> BuildFor(IEnumerable<object> objects);
    }

    abstract class BasedTypedInternalBuilder<T> : InternalBuilder
    {
        public override IAsyncEnumerable<KeyValuePair<Type, object>> BuildFor(IEnumerable<object> objects)
        {
            var result = new KeyValuePair<Type, object>(typeof(T), InternalBuild(objects));
            return AsyncEnumerable.Empty<KeyValuePair<Type, object>>().Append(result);
        }
        protected abstract object InternalBuild(IEnumerable<object> objects);
    }

    class ServiceProvidedBuilder<T> : BasedTypedInternalBuilder<T>
        where T : notnull
    {
        protected override object InternalBuild(IEnumerable<object> objects)
        {
            var provider = objects.OfType<IServiceProvider>().First();
            return provider.GetRequiredService<T>();
        }
    }

    class InstanceBuilder<T> : BasedTypedInternalBuilder<T>
        where T : notnull
    {
        object _instance;
        public InstanceBuilder(T instance)
        {
            _instance = instance;
        }

        protected override object InternalBuild(IEnumerable<object> objects)
        {
            return _instance;
        }
    }

    class FactoryBuilder<U, T> : BasedTypedInternalBuilder<T>
        where T : notnull
        where U : notnull
    {
        Func<U, T> _factory;

        public FactoryBuilder(Func<U, T> factory)
        {
            _factory = factory;
        }

        protected override object InternalBuild(IEnumerable<object> items)
        {
            var u = ExtractType<U>(items);
            return _factory(u);
        }
    }

    class FactoryAsyncBuilder<U, T> : InternalBuilder
        where T : notnull
        where U : notnull
    {
        Func<U, Task<T>> _factory;

        public FactoryAsyncBuilder(Func<U, Task<T>> factory)
        {
            _factory = factory;
        }

        public override async IAsyncEnumerable<KeyValuePair<Type, object>> BuildFor(IEnumerable<object> items)
        {
            var u = ExtractType<U>(items);
            var t = await _factory(u);
            yield return new KeyValuePair<Type, object>(typeof(T), t);

        }
    }

    class NestedBuilder : InternalBuilder
    {
        ApplicationContextBuilder _builder;
        public NestedBuilder(ApplicationContextBuilder builder)
        {
            _builder = builder;
        }

        public override async IAsyncEnumerable<KeyValuePair<Type, object>> BuildFor(IEnumerable<object> objects)
        {
            foreach (var kvp in await _builder.BuildInternalAsync(objects))
            {
                yield return kvp;
            }
        }
    }

    List<InternalBuilder> builders = new();

    public ApplicationContextBuilder AddFromService<T>()
        where T : notnull
    {
        builders.Add(new ServiceProvidedBuilder<T>());
        return this;
    }

    public ApplicationContextBuilder Add<T>(T instance)
        where T : notnull
    {
        builders.Add(new InstanceBuilder<T>(instance));
        return this;
    }

    public void AddFactory<T, U>(Func<T, U> setupFunc)
        where U : notnull
        where T : notnull
    {
        builders.Add(new FactoryBuilder<T, U>(setupFunc));
    }

    public void AddAsyncFactory<T, U>(Func<T, Task<U>> setupFunc)
        where U : notnull
        where T : notnull
    {
        builders.Add(new FactoryAsyncBuilder<T, U>(setupFunc));
    }

    public ApplicationContextBuilder AddBuilder(Action<ApplicationContextBuilder> builder)
    {
        ApplicationContextBuilder myBuilder = new();
        builder(myBuilder);
        return AddBuilder(myBuilder);
    }

    public ApplicationContextBuilder AddBuilder(ApplicationContextBuilder builder)
    {
        var nestedBuilder = new NestedBuilder(builder);
        builders.Add(nestedBuilder);
        return this;
    }

    async Task<ImmutableDictionary<Type, object>> BuildInternalAsync(IEnumerable<object> uses)
    {
        var kvps = await builders
            .ToAsyncEnumerable()
            .SelectMany(b => b.BuildFor(uses))
            .ToDictionaryAsync(kvp => kvp.Key, kvp => kvp.Value);
        return kvps.ToImmutableDictionary();
    }

    public async Task<ImmutableDictionary<Type, object>> BuildAsync(IServiceProvider serviceProvider, params object[] objects)
    {
        var all = objects.Prepend(serviceProvider);
        var dict =  await BuildInternalAsync(all);
        return dict.Add(typeof(IServiceProvider), serviceProvider);
    }

}