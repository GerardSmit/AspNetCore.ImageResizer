using GerardSmit.AspNetCore.ImageResizer.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GerardSmit.AspNetCore.ImageResizer;

public static class ImageSharp3Extensions
{
    public static ImageResizeBuilder UseImageSharp3(this ImageResizeBuilder builder)
    {
        builder.Services.AddSingleton<IImageResizer, ImageSharp3ImageResizer>();
        return builder;
    }
}
