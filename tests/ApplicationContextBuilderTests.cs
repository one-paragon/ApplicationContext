using Xunit;
using System.Threading.Tasks;
using OneParagon.Infrasctucture;
using Microsoft.Extensions.DependencyInjection;

namespace tests;

public class ApplicationContextBuilderTests
{  

    [Fact]
    public async Task SimpleCaseWithBuilder()
    {
        var builder = new ApplicationContextBuilder();
        builder.Add("hello");
        IServiceCollection sc = new ServiceCollection();
        var sp = sc.BuildServiceProvider();
        var ctx = await builder.BuildAsync<object>(sp);

        ApplicationContext.SetFeatures(ctx);

        Assert.Equal("hello",ApplicationContext.GetFeature<string>());
    }

    [Fact]
    public async Task BuilderWithDefault()
    {
        var builder = new ApplicationContextBuilder();
        builder.Add("hello");
        IServiceCollection sc = new ServiceCollection();
        var sp = sc.BuildServiceProvider();
        var ctx = await builder.BuildAsync<object>(sp);

        ApplicationContext.DefaultFeatures = ctx;

        Assert.Equal("hello",ApplicationContext.GetFeature<string>());
    }

    [Fact]
    public async Task MultipeBuildersDefault()
    {
        var builder = new ApplicationContextBuilder();
        builder.Add("hello");
        builder.WithBuilderFor<int>( builder => builder.Add("bye"));
        IServiceCollection sc = new ServiceCollection();
        var sp = sc.BuildServiceProvider();
        var ctx = await builder.BuildAsync<object>(sp);

        ApplicationContext.SetFeatures(ctx);

        Assert.Equal("hello",ApplicationContext.GetFeature<string>());

        ctx = await builder.BuildAsync<int>(sp);

        ApplicationContext.SetFeatures(ctx);

        Assert.Equal("bye",ApplicationContext.GetFeature<string>());
    }
}