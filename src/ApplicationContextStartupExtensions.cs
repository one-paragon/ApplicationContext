using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OneParagon.Infrasctucture;
public static class ApplicationContextStartupExtensions
{

    public static ApplicationContextBuilder AddApplicationContext(this IServiceCollection services, Action<ApplicationContextBuilder> applicationContextBuilder)
    {
        ApplicationContextBuilder builder = new();
        services.AddSingleton(builder);
        applicationContextBuilder(builder);
        return builder;
    }

    public static IApplicationBuilder UseApplicationContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApplicationContextMiddleware>();
    }

    public static async Task UseDefaultApplicatonContext(this IHost host)
    {
        var builder = host.Services.GetRequiredService<ApplicationContextBuilder>();
        var contextFeatures = await builder.BuildAsync<object>(host.Services);
        ApplicationContext.DefaultFeatures = contextFeatures;
    }

    public static ApplicationContextBuilder WithMiddleware(this ApplicationContextBuilder contextBuilder, Action<ApplicationContextMiddlewareBuilder> middlewareBuilder)
    {
        return contextBuilder.WithBuilderFor<ApplicationContextMiddleware, ApplicationContextMiddlewareBuilder>(middlewareBuilder);
    }

}