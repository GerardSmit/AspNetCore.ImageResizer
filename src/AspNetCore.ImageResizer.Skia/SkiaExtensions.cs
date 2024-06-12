using GerardSmit.AspNetCore.ImageResizer.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GerardSmit.AspNetCore.ImageResizer;

public static class SkiaExtensions
{
    public static ImageResizeBuilder UseSkia(this ImageResizeBuilder builder)
    {
        builder.Services.AddSingleton<IImageResizer, SkiaImageResizer>();
        return builder;
    }
}
