using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace OneParagon.Infrasctucture;

public class ApplicationContextBuilder
{
    abstract class InternalBuilder
    {
        public Type ShouldBuildFor = typeof(object);
        public abstract IAsyncEnumerable<KeyValuePair<Type, object>> BuildFor<T>(IEnumerable<object> stuff);
    }



    abstract class BasedTypedInternalBuilder<T> : InternalBuilder
    {

        public override IAsyncEnumerable<KeyValuePair<Type, object>> BuildFor<T1>(IEnumerable<object> stuff)
        {
            if (typeof(T1) == ShouldBuildFor)
            {
                var result = new KeyValuePair<Type, object>(typeof(T), InternalBuild(stuff));
                return AsyncEnumerable.Empty<KeyValuePair<Type, object>>().Append(result);
            }
            return AsyncEnumerable.Empty<KeyValuePair<Type, object>>();
        }

        protected abstract object InternalBuild(IEnumerable<object> stuff);
    }

    class ServiceProvidedBuilder<T> : BasedTypedInternalBuilder<T>
        where T : notnull
    {
        protected override object InternalBuild(IEnumerable<object> stuff)
        {
            var provider = stuff.OfType<IServiceProvider>().First();
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

        protected override object InternalBuild(IEnumerable<object> stuff)
        {
            return _instance;
        }
    }

    class FactoryBuilder<U, T> : BasedTypedInternalBuilder<T>
        where T : notnull
    {
        Func<U, T> _factory;

        public FactoryBuilder(Func<U, T> factory)
        {
            _factory = factory;
        }

        protected override object InternalBuild(IEnumerable<object> items)
        {
            var u = items.OfType<U>().First();
            return _factory(u);
        }
    }

    class FactoryAsyncBuilder<U, T> : InternalBuilder
        where T : notnull
    {
        Func<U, Task<T>> _factory;

        public FactoryAsyncBuilder(Func<U, Task<T>> factory)
        {
            _factory = factory;
        }

        public override async IAsyncEnumerable<KeyValuePair<Type, object>> BuildFor<T1>(IEnumerable<object> items)
        {
            if (typeof(T1) == ShouldBuildFor)
            {
                var u = items.OfType<U>().First();
                var t = await _factory(u);
                yield return new KeyValuePair<Type, object>(typeof(T), t);
            }
        }
    }

    class NestedBuilder : InternalBuilder
    {
        ApplicationContextBuilder _builder;
        public NestedBuilder(ApplicationContextBuilder builder)
        {
            _builder = builder;
        }

        public override async IAsyncEnumerable<KeyValuePair<Type, object>> BuildFor<T>(IEnumerable<object> stuff)
        {
            if (typeof(T) == ShouldBuildFor)
            {
                foreach (var kvp in await _builder.BuildInternalAsync<T>(stuff))
                {
                    yield return kvp;
                }
            }
        }
    }

    Type myType = typeof(object);

    List<InternalBuilder> builders = new();

    void AddInternal(InternalBuilder builder)
    {
        builders.Add(builder);
        builder.ShouldBuildFor = myType;
    }
    public ApplicationContextBuilder Add<T>()
        where T : notnull
    {
        AddInternal(new ServiceProvidedBuilder<T>());
        return this;
    }

    public ApplicationContextBuilder Add<T>(T instance)
        where T : notnull
    {
        AddInternal(new InstanceBuilder<T>(instance));
        return this;
    }

    public ApplicationContextBuilder Clone()
    {
        var builder = new ApplicationContextBuilder();
        builder.builders.AddRange(builders);
        return builder;
    }

    public void Merge(ApplicationContextBuilder builder)
    {
        builders.AddRange(builder.builders);
    }

    public void AddFactory<T, U>(Func<T, U> setupFunc)
        where U : notnull
    {
        AddInternal(new FactoryBuilder<T, U>(setupFunc));
    }

    public void AddAsyncFactory<T, U>(Func<T, Task<U>> setupFunc)
        where U : notnull
    {
        AddInternal(new FactoryAsyncBuilder<T, U>(setupFunc));
    }

    void AddBuilderFor<T>(ApplicationContextBuilder builder)
    {
        foreach (var b in builder.builders)
        {
            b.ShouldBuildFor = typeof(T);
            builders.Add(b);
        }
    }


    async Task<ImmutableDictionary<Type, object>> BuildInternalAsync<T>(IEnumerable<object> uses)
    {
        var kvps = await builders
            .ToAsyncEnumerable()
            .SelectMany(b => b.BuildFor<T>(uses))
            .ToDictionaryAsync(kvp => kvp.Key, kvp => kvp.Value);
        return kvps.ToImmutableDictionary();
    }



    public async Task<ImmutableDictionary<Type, object>> BuildAsync<T>(IServiceProvider serviceProvider, params object[] uses)
    {
        var all = uses.Prepend(serviceProvider);
        return await BuildInternalAsync<T>(all);
    }

    public ApplicationContextBuilder WithBuilderFor<T>(Action<ApplicationContextBuilder> builder)
    {
        ApplicationContextBuilder myBuilder = new();
        myBuilder.myType = typeof(T);
        builder(myBuilder);
        var nestedBuilder = new NestedBuilder(myBuilder);
        nestedBuilder.ShouldBuildFor = myBuilder.myType;
        builders.Add(nestedBuilder);
        return this;
    }

    public ApplicationContextBuilder WithBuilderFor<T, TBuilder>(Action<TBuilder> builder)
       where TBuilder : ApplicationContextBuilder, new()
    {
        TBuilder myBuilder = new();
        myBuilder.myType = typeof(T);
        builder(myBuilder);
        var nestedBuilder = new NestedBuilder(myBuilder);
        nestedBuilder.ShouldBuildFor = myBuilder.myType;
        builders.Add(nestedBuilder);
        return this;
    }
}