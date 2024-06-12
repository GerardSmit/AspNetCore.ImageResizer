using GerardSmit.AspNetCore.ImageResizer.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GerardSmit.AspNetCore.ImageResizer;

public static class AspNetCoreExtensions
{
    public static IServiceCollection AddImageResizer(this IServiceCollection services, Action<ImageResizeBuilder>? configure = null)
    {
        services.TryAddSingleton<ImageResizeMiddleware>();
        configure?.Invoke(new ImageResizeBuilder(services));
        return services;
    }

    public static IApplicationBuilder UseImageResizer(this IApplicationBuilder app)
    {
        app.UseMiddleware<ImageResizeMiddleware>();
        return app;
    }
}

public class ImageResizeBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;

    public void Configure(Action<ImageResizerOptions> configure)
    {
        Services.Configure(configure);
    }
}