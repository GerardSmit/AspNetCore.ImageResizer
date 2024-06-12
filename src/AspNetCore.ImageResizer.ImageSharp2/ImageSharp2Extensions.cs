using GerardSmit.AspNetCore.ImageResizer.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GerardSmit.AspNetCore.ImageResizer;

public static class ImageSharp2Extensions
{
    public static ImageResizeBuilder UseImageSharp2(this ImageResizeBuilder builder)
    {
        builder.Services.AddSingleton<IImageResizer, ImageSharp2ImageResizer>();
        return builder;
    }
}
