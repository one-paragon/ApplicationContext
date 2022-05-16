using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OneParagon.Infrastructure;

public static class ApplicationContextStartupExtensions
{

    public static ApplicationContextBuilder AddDefaultApplicationContext(this IServiceCollection services, Action<ApplicationContextBuilder> applicationContextBuilder)
    {
        ApplicationContextBuilder builder = new();
        services.AddSingleton(builder);
        applicationContextBuilder(builder);
        return builder;
    }

    public static IApplicationBuilder UseApplicationContext(this IApplicationBuilder builder)
    {
        var defaultBuilder = builder.ApplicationServices.GetService<ApplicationContextBuilder>();
        return builder.UseMiddleware<ApplicationContextMiddleware>(defaultBuilder);
    }


    public static IApplicationBuilder UseApplicationContext(this IApplicationBuilder appBuilder, Action<ApplicationContextBuilder>? contextBuilder = null ) 
    {
        var  builder = appBuilder.MergeAllBuilders(contextBuilder);
        
        return appBuilder.UseMiddleware<ApplicationContextMiddleware>(builder);
    }

    public static ApplicationContextBuilder MergeAllBuilders(this IApplicationBuilder appBuilder, Action<ApplicationContextBuilder>? contextBuilder = null ) {
        var existingBuilder = appBuilder.ApplicationServices.GetService<ApplicationContextBuilder>();

        var builder = new ApplicationContextBuilder();

        if(existingBuilder is not null) {
            builder.AddBuilder(existingBuilder);
        }

        if(contextBuilder is not null) {
            contextBuilder(builder);
        }

        return builder;
    }

    public static IApplicationBuilder UsePostAuthenticationApplicationContext(this IApplicationBuilder appBuilder, Action<ApplicationContextBuilder>? contextBuilder = null ) 
    {
        var builder = appBuilder.MergeAllBuilders(contextBuilder);        
        return appBuilder.UseWhen( context => context.User.Identity?.IsAuthenticated ?? false, appBuilder => {
            appBuilder.UseMiddleware<ApplicationContextMiddleware>(builder);
        });
    }

    public static async Task InitDefaultApplicatonContext(this IHost host)
    {
        var builder = host.Services.GetRequiredService<ApplicationContextBuilder>();
        var contextFeatures = await builder.BuildAsync(host.Services);
        ApplicationContext.DefaultFeatures = contextFeatures;
    }

}