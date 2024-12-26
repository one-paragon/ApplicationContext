using Xunit;
using System.Threading.Tasks;
using OneParagon.Infrastructure;
using System.Collections.Immutable;

namespace tests;

public class ApplicationContextTests
{
    [Fact]
    public async Task SimpleCase()
    {
        ApplicationContext.SetFeature("hello");
        Assert.Equal("hello", ApplicationContext.GetFeature<string>());
    }

    [Fact]
    public async Task OverWrites()
    {
        ApplicationContext.SetFeature("hello");
        Assert.Equal("hello", ApplicationContext.GetFeature<string>());
        ApplicationContext.SetFeature("bye");
        Assert.Equal("bye", ApplicationContext.GetFeature<string>());
    }

    [Fact]
    public async Task OverWriteWithAsync()
    {
        ApplicationContext.SetFeature("hello");
        Assert.Equal("hello", ApplicationContext.GetFeature<string>());

        await Task.Run(async () =>
        {
            ApplicationContext.SetFeature("bye");
            await Task.Delay(1);
            Assert.Equal("bye", ApplicationContext.GetFeature<string>());
        });

        Assert.Equal("hello", ApplicationContext.GetFeature<string>());

    }

    [Fact]
    public async Task OverWriteDoesNotResetMainThread()
    {
        ApplicationContext.SetFeature("hello");
        Assert.Equal("hello", ApplicationContext.GetFeature<string>());
        TaskCompletionSource finished = new TaskCompletionSource();
        TaskCompletionSource taskFinished = new TaskCompletionSource();
        var t1 = Task.Run(async () =>
        {
            ApplicationContext.SetFeature("bye");
            taskFinished.SetResult();
            await finished.Task;
            Assert.Equal("bye", ApplicationContext.GetFeature<string>());
        });
        await taskFinished.Task;
        Assert.Equal("hello", ApplicationContext.GetFeature<string>());
        ApplicationContext.SetFeature("world");
        finished.SetResult();
        await t1;
        Assert.Equal("world", ApplicationContext.GetFeature<string>());

    }

    [Fact]
    public async Task OverWriteWithTwoAsyncTask()
    {
        ApplicationContext.SetFeature("hello");
        Assert.Equal("hello", ApplicationContext.GetFeature<string>());
        TaskCompletionSource reset1 = new TaskCompletionSource();
        TaskCompletionSource mainFinished = new TaskCompletionSource();
        TaskCompletionSource firstTaskFinished = new TaskCompletionSource();
        var t1 = Task.Run(async () =>
        {
            await reset1.Task;
            Assert.Equal("hello", ApplicationContext.GetFeature<string>());
            ApplicationContext.SetFeature("bye");
            firstTaskFinished.SetResult();
            await mainFinished.Task;
            Assert.Equal("bye", ApplicationContext.GetFeature<string>());
        });

        ApplicationContext.SetFeature("hello again");

        reset1.SetResult();

        var t2 = Task.Run(async () =>
        {
            Assert.Equal("hello again", ApplicationContext.GetFeature<string>());
            await firstTaskFinished.Task;
            Assert.Equal("hello again", ApplicationContext.GetFeature<string>());
        });
        await firstTaskFinished.Task;
        Assert.Equal("hello again", ApplicationContext.GetFeature<string>());
        ApplicationContext.SetFeature("world");
        mainFinished.SetResult();
        await Task.WhenAll(t1, t2);
        Assert.Equal("world", ApplicationContext.GetFeature<string>());

    }

    [Fact]
    public async Task OverWriteWithNonAsyncTasks()
    {
        ApplicationContext.SetFeature("hello");
        Assert.Equal("hello", ApplicationContext.GetFeature<string>());
        TaskCompletionSource reset1 = new TaskCompletionSource();

        var t1 = Task.Run(() =>
        {
            Assert.Equal("hello", ApplicationContext.GetFeature<string>());
            ApplicationContext.SetFeature("bye");
            return reset1.Task.ContinueWith(t =>
            {
                Assert.Equal("bye", ApplicationContext.GetFeature<string>());
            });
        });

        ApplicationContext.SetFeature("hello again");

        reset1.SetResult();
        await Task.WhenAll(t1);
        Assert.Equal("hello again", ApplicationContext.GetFeature<string>());

    }

    [Fact]
    public void AddKeyedFeature()
    {
        ApplicationContext.SetKeyedFeature<int[]>("key", new int[] { 1, 2, 3 });
        var result = ApplicationContext.GetKeyedFeature<int[]>("key");
        Assert.Collection(result, (x) => Assert.Equal(1, x), (x2) => Assert.Equal(2, x2), (x3) => Assert.Equal(3, x3));
    }

    [Fact]
    public void ResetContext()
    {
        ApplicationContext.SetFeature("myFeature");
        ApplicationContext.SetKeyedFeature("key", new int[] { 1, 2, 3 });

        var result = ApplicationContext.ResetContext();
        Assert.Collection(result, (x) => Assert.Equal("myFeature", x.Value), (x2) => Assert.IsType<ImmutableDictionary<string, int[]>>(x2.Value));
    }
}