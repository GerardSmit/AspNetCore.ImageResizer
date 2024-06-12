namespace GerardSmit.AspNetCore.ImageResizer.Abstractions;

public interface IImageResizer
{
    Task ResizeAsync(Stream source, Stream destination, ResizeParams resizeParams);
}
