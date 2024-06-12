using System.Drawing;
using GerardSmit.AspNetCore.ImageResizer.Abstractions;
using SkiaSharp;

namespace GerardSmit.AspNetCore.ImageResizer;

public class SkiaImageResizer : IImageResizer
{
    public Task ResizeAsync(Stream source, Stream destination, ResizeParams resizeParams)
    {
        var bitmap = LoadBitmap(source);
        var (size, imageRectangle, cropRectangle) = resizeParams.GetResult(new Size(bitmap.Width, bitmap.Height));

        // Crop the image if needed
        if (cropRectangle.HasValue)
        {
            var newBitmap = new SKBitmap(cropRectangle.Value.Width, cropRectangle.Value.Height);
            bitmap.ExtractSubset(newBitmap, new SKRectI(cropRectangle.Value.Left, cropRectangle.Value.Top, cropRectangle.Value.Right, cropRectangle.Value.Bottom));
            bitmap.Dispose();
            bitmap = newBitmap;
        }

        // Resize the image
        var resizedImageInfo = new SKImageInfo(size.Width, size.Height, SKImageInfo.PlatformColorType, bitmap.AlphaType);
        using var resizedBitmap = new SKBitmap(resizedImageInfo);
        using var canvas = new SKCanvas(resizedBitmap);

        canvas.DrawBitmap(bitmap, new SKRect(imageRectangle.Left, imageRectangle.Top, imageRectangle.Right, imageRectangle.Bottom));

        var format = resizeParams.Format switch
        {
            ImageFormat.Png => SKEncodedImageFormat.Png,
            ImageFormat.Webp => SKEncodedImageFormat.Webp,
            ImageFormat.Jpeg => SKEncodedImageFormat.Jpeg,
            _ => throw new NotSupportedException($"The image extension {resizeParams.Format} is not supported.")
        };

        using var resizedImage = SKImage.FromBitmap(resizedBitmap);
        using var imageData = resizedImage.Encode(format, resizeParams.Quality);

        imageData.SaveTo(destination);

        return Task.CompletedTask;
    }

    private static SKBitmap LoadBitmap(Stream stream)
    {
        using var s = new SKManagedStream(stream);
        using var codec = SKCodec.Create(s);

        var info = codec.Info;
        var bitmap = new SKBitmap(info.Width, info.Height, SKImageInfo.PlatformColorType, info.IsOpaque ? SKAlphaType.Opaque : SKAlphaType.Premul);

        var result = codec.GetPixels(bitmap.Info, bitmap.GetPixels(out _));

        if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput)
        {
            throw new ArgumentException("Unable to load bitmap from provided data");
        }

        return bitmap;
    }
}
