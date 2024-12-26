using Xunit;
using System.Threading.Tasks;
using OneParagon.Infrastructure;
using System.Threading;
using System;
using System.Collections.Generic;

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
    public void CaptureContext()
    {
        ApplicationContext.SetFeature("myFeature");
        ApplicationContext.SetKeyedFeature("key", new int[] { 1, 2, 3 });

        var result = ApplicationContext.CaptureContext();
        Assert.Collection(result,
            (x) => Assert.Equal("myFeature", x.Value),
            (x2) => Assert.IsType<KeyValuePair<Type, object>>(x2)
        );
    }

    [Fact]
    public void WrapInContext_Task()
    {
        var stringToSetOnThread = "";

        var task = async () =>
        {
            stringToSetOnThread = $"Feature from {ApplicationContext.GetFeature<string>()}";
            await Task.CompletedTask;
        };
        var wrappedTask = () => Task.CompletedTask;
        var context1 = new Thread(async () =>
        {
            ApplicationContext.SetFeature("context1");
            wrappedTask = ApplicationContext.WrapInContext(task);
            await wrappedTask();
        });

        var context2 = new Thread(async () =>
        {
            await wrappedTask();
            stringToSetOnThread = $"{stringToSetOnThread} and executed on context2.";
        });

        context1.Start();
        context1.Join();
        Assert.Equal("Feature from context1", stringToSetOnThread);

        context2.Start();
        context2.Join();
        Assert.Equal("Feature from context1 and executed on context2.", stringToSetOnThread);
    }

    [Fact]
    public void WrapInContextT1_Output()
    {
        var task = () => Task.FromResult($"Feature from {ApplicationContext.GetFeature<string>()}");
        var wrappedTask = () => Task.FromResult("");

        var result = "";
        var context1 = new Thread(async () =>
        {
            ApplicationContext.SetFeature("context1");
            wrappedTask = ApplicationContext.WrapInContext(task);
            result = await wrappedTask();
        });

        var context2 = new Thread(async () =>
        {
            var x = await wrappedTask();
            result = $"{x} and executed on context2.";
        });

        context1.Start();
        context1.Join();
        Assert.Equal("Feature from context1", result);

        context2.Start();
        context2.Join();
        Assert.Equal("Feature from context1 and executed on context2.", result);
    }

    [Fact]
    public void WrapInContext_Input_Output()
    {
        var task = (string argument) => Task.FromResult($"Feature from {ApplicationContext.GetFeature<string>()} with input '{argument}'");
        var wrappedTask = (string arg) => Task.FromResult(arg);

        var result = "";
        var context1 = new Thread(async () =>
        {
            ApplicationContext.SetFeature("context1");
            wrappedTask = ApplicationContext.WrapInContext(task);
            result = await wrappedTask("this is a context1 argument");
        });

        var context2 = new Thread(async () =>
        {
            var x = await wrappedTask("this is a context2 argument");
            result = $"{x} and executed on context2.";
        });

        context1.Start();
        context1.Join();
        Assert.Equal("Feature from context1 with input 'this is a context1 argument'", result);

        context2.Start();
        context2.Join();
        Assert.Equal("Feature from context1 with input 'this is a context2 argument' and executed on context2.", result);
    }

    [Fact]
    public void WrapInContext_Input_Task()
    {
        var x = "";
        var task = async (string argument) =>
        {
            x = $"Feature from {ApplicationContext.GetFeature<string>()} with input '{argument}'";
            await Task.CompletedTask;
        };
        var wrappedTask = (string arg) => Task.CompletedTask;

        var result = "";
        var context1 = new Thread(async () =>
        {
            ApplicationContext.SetFeature("context1");
            wrappedTask = ApplicationContext.WrapInContext(task);
            await wrappedTask("this is a context1 argument");
        });

        var context2 = new Thread(async () =>
        {
            await wrappedTask("this is a context2 argument");
            result = $"{x} and executed on context2.";
        });

        context1.Start();
        context1.Join();
        Assert.Equal("Feature from context1 with input 'this is a context1 argument'", x);

        context2.Start();
        context2.Join();
        Assert.Equal("Feature from context1 with input 'this is a context2 argument' and executed on context2.", result);
    }
}