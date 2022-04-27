#nullable enable

using Microsoft.AspNetCore.Http;

namespace OneParagon.Infrasctucture;

public class ApplicationContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApplicationContextBuilder _builer;

    public ApplicationContextMiddleware(RequestDelegate next, ApplicationContextBuilder builder)
    {
        _next = next;
        _builer = builder;
    }

    public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        var contextFeatues = await _builer.BuildAsync(serviceProvider);
        ApplicationContext.SetFeatures(contextFeatues);
        await _next(context);
    }
}
