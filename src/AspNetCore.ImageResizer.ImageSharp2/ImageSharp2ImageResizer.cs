using GerardSmit.AspNetCore.ImageResizer.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Size = System.Drawing.Size;

namespace GerardSmit.AspNetCore.ImageResizer;

public class ImageSharp2ImageResizer : IImageResizer
{
    public async Task ResizeAsync(Stream source, Stream destination, ResizeParams resizeParams)
    {
        var image = await Image.LoadAsync(source);
        var (size, imageRectangle, cropRectangle) = resizeParams.GetResult(new Size(image.Width, image.Height));

        // Crop the image if needed
        if (cropRectangle.HasValue)
        {
            image.Mutate(context =>
            {
                context.Crop(new Rectangle(
                    cropRectangle.Value.Left,
                    cropRectangle.Value.Top,
                    cropRectangle.Value.Width,
                    cropRectangle.Value.Height));
            });
        }

        // Resize the image
        using var resizedImageInfo = new Image<Rgba32>(size.Width, size.Height);

        image.Mutate(context =>
        {
            context.Resize(imageRectangle.Width, imageRectangle.Height);
        });

        resizedImageInfo.Mutate(context =>
        {
            context.DrawImage(
                image,
                new Point(imageRectangle.Left, imageRectangle.Top),
                1f);
        });

        IImageEncoder format = resizeParams.Format switch
        {
            ImageFormat.Png => new PngEncoder(),
            ImageFormat.Webp => new WebpEncoder { Quality = resizeParams.Quality },
            ImageFormat.Jpeg => new JpegEncoder { Quality = resizeParams.Quality },
            _ => throw new NotSupportedException($"The image extension {resizeParams.Format} is not supported.")
        };


        await resizedImageInfo.SaveAsync(destination, format);
    }
}
