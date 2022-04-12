using Xunit;
using System.Threading.Tasks;
using OneParagon.Infrasctucture;
using Microsoft.Extensions.DependencyInjection;

namespace tests;

public class ApplicationContextTests
{
    [Fact]
    public async Task SimpleCase()
    {
        // var builder = new ApplicationContextBuilder();
        // builder.Add("hello");
        // IServiceCollection sc = new ServiceCollection();
        // var sp = sc.BuildServiceProvider();
        // var ctx = await builder.BuildAsync<object>(sp);

        ApplicationContext.SetFeature("hello");
        // ApplicationContext.SetFeatures(ctx);

        Assert.Equal("hello",ApplicationContext.GetFeature<string>());
    }

    [Fact]
    public async Task OverWrites()
    {
        ApplicationContext.SetFeature("hello");
        Assert.Equal("hello",ApplicationContext.GetFeature<string>());
        ApplicationContext.SetFeature("bye");
        Assert.Equal("bye",ApplicationContext.GetFeature<string>());
    }

    [Fact]
    public async Task OverWriteWithAsync()
    {
        ApplicationContext.SetFeature("hello");
        Assert.Equal("hello",ApplicationContext.GetFeature<string>());

        await Task.Run( async () => {
            ApplicationContext.SetFeature("bye");
            await Task.Delay(1);            
            Assert.Equal("bye",ApplicationContext.GetFeature<string>());
        });

        Assert.Equal("hello",ApplicationContext.GetFeature<string>());
        
    }

}