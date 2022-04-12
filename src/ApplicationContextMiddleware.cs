#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace OneParagon.Infrasctucture;


public class ApplicationContextMiddleware
{
    private readonly RequestDelegate _next;

    public ApplicationContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        var builder = serviceProvider.GetService<ApplicationContextBuilder>();
        if (builder is null)
        {
            throw new InvalidOperationException("The services for ApplicationContext have not been registered. In startup register them by calling services.AddApplicationContext()");
        }
        var contextFeatues = await builder.BuildAsync<ApplicationContextMiddleware>(serviceProvider, context);
        ApplicationContext.SetFeatures(contextFeatues);
        await _next(context);
    }
}
