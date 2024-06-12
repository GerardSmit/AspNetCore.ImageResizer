namespace GerardSmit.AspNetCore.ImageResizer;

public enum ImageFormat : byte
{
    Png,
    Jpeg,
    Webp
}

public static class ImageFormatExtensions
{
    public static string GetMimeType(this ImageFormat format)
    {
        return format switch
        {
            ImageFormat.Png => "image/png",
            ImageFormat.Webp => "image/webp",
            ImageFormat.Jpeg => "image/jpeg",
            _ => throw new NotSupportedException($"The image extension {format} is not supported.")
        };
    }
}