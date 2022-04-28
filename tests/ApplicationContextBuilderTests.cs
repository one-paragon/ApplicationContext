using Xunit;
using System.Threading.Tasks;
using OneParagon.Infrasctucture;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace tests;

public class ApplicationContextBuilderTests
{
    [Fact]
    public async Task ContextBuilder_AutoAdds_ServiceProvider() {
        var builder = new ApplicationContextBuilder();
        IServiceCollection sc = new ServiceCollection();
        var sp = sc.BuildServiceProvider();

        var ctx = await builder.BuildAsync(sp);

        ApplicationContext.SetFeatures(ctx);

        Assert.Equal(sp,ApplicationContext.GetFeature<IServiceProvider>());
    }

    [Fact]
    public async Task InstanceBuilder()
    {
        var builder = new ApplicationContextBuilder();

        IServiceCollection sc = new ServiceCollection();
        var sp = sc.BuildServiceProvider();


        builder.Add("hello");
        var ctx = await builder.BuildAsync(sp);
        ApplicationContext.SetFeatures(ctx);
        
        Assert.Equal("hello",ApplicationContext.GetFeature<string>());
    }

    [Fact]
    public async Task ServiceProvidedBuilder()
    {
        var builder = new ApplicationContextBuilder();

        IServiceCollection sc = new ServiceCollection();
        sc.AddSingleton("hello");
        var sp = sc.BuildServiceProvider();


        builder.AddFromService<string>();
        var ctx = await builder.BuildAsync(sp);
        ApplicationContext.SetFeatures(ctx);
        
        Assert.Equal("hello",ApplicationContext.GetFeature<string>());
    }

    [Fact]
    public async Task FactoryBuilder()
    {
        var builder = new ApplicationContextBuilder();

        IServiceCollection sc = new ServiceCollection();
        sc.AddSingleton("hello");
        var sp = sc.BuildServiceProvider();


        builder.AddFactory( (IServiceProvider sp) => sp.GetRequiredService<string>() );

        var ctx = await builder.BuildAsync(sp);
        ApplicationContext.SetFeatures(ctx);
        
        Assert.Equal("hello",ApplicationContext.GetFeature<string>());
    }

    [Fact]
    public async Task AsyncFactoryBuilder()
    {
        var builder = new ApplicationContextBuilder();

        IServiceCollection sc = new ServiceCollection();
        sc.AddSingleton("hello");
        var sp = sc.BuildServiceProvider();


        builder.AddAsyncFactory( async (IServiceProvider sp) => {
            await Task.Delay(1);
            return sp.GetRequiredService<string>();
        });
        
        var ctx = await builder.BuildAsync(sp);
        ApplicationContext.SetFeatures(ctx);
        
        Assert.Equal("hello",ApplicationContext.GetFeature<string>());
    }

  [Fact]
    public async Task FactoryBuilderFromService()
    {
        var builder = new ApplicationContextBuilder();

        IServiceCollection sc = new ServiceCollection();
        sc.AddSingleton("hello");
        var sp = sc.BuildServiceProvider();


        builder.AddFactory( (string s) => s + " world" );

        var ctx = await builder.BuildAsync(sp);
        ApplicationContext.SetFeatures(ctx);
        
        Assert.Equal("hello world",ApplicationContext.GetFeature<string>());
    }

    [Fact]
    public async Task AsyncFactoryBuilderFromService()
    {
        var builder = new ApplicationContextBuilder();

        IServiceCollection sc = new ServiceCollection();
        sc.AddSingleton("hello");
        var sp = sc.BuildServiceProvider();


        builder.AddAsyncFactory( async (string s) => {
            await Task.Delay(1);
            return  s + " world";
        });
        
        var ctx = await builder.BuildAsync(sp);
        ApplicationContext.SetFeatures(ctx);
        
        Assert.Equal("hello world",ApplicationContext.GetFeature<string>());
    }



}