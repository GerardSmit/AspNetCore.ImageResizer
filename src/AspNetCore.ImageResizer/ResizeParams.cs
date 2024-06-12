using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace GerardSmit.AspNetCore.ImageResizer;

public readonly record struct ResizeParams(int Width, int Height, byte Quality, ImageFormat Format, ResizeMode Mode)
{
    public Size Size => new(Width, Height);

    public ImageResult GetResult(Size originalSize)
    {
        var width = Width;
        var height = Height;

        if (height == 0)
        {
            height = (int)Math.Round(originalSize.Height * (float)width / originalSize.Width);
        }
        else if (width == 0)
        {
            width = (int)Math.Round(originalSize.Width * (float)height / originalSize.Height);
        }

        return Mode switch
        {
            ResizeMode.Crop => GetCropResult(originalSize, width, height),
            ResizeMode.Max => GetMaxRectangle(originalSize, width, height),
            ResizeMode.Pad => GetPadResult(originalSize, width, height),
            _ => GetDefaultResult(originalSize, width, height)
        };
    }

    private static ImageResult GetDefaultResult(Size originalSize, int width, int height)
    {
        return new ImageResult(
            Size: new Size(width, height),
            ImageRectangle: new Rectangle(0, 0, width, height)
        );
    }

    private static ImageResult GetMaxRectangle(Size originalSize, int width, int height)
    {
        var ratio = Math.Min((float)width / originalSize.Width, (float)height / originalSize.Height);
        var newWidth = (int)Math.Round(originalSize.Width * ratio);
        var newHeight = (int)Math.Round(originalSize.Height * ratio);

        return new ImageResult(
            Size: new Size(newWidth, newHeight),
            ImageRectangle: new Rectangle(0, 0, newWidth, newHeight)
        );
    }

    private static ImageResult GetPadResult(Size originalSize, int width, int height)
    {
        var bitmapRatio = (float)originalSize.Width / originalSize.Height;
        var resizeRatio = (float)width / height;

        int imageWidth;
        int imageHeight;
        int left = 0;
        int top = 0;

        if (bitmapRatio > resizeRatio)
        {
            imageWidth = width;
            imageHeight = (int)Math.Round(originalSize.Height * ((float)width / originalSize.Width));
            top = (height - imageHeight) / 2;
        }
        else
        {
            imageWidth = (int)Math.Round(originalSize.Width * ((float)height / originalSize.Height));
            imageHeight = height;
            left = (width - imageWidth) / 2;
        }

        return new ImageResult(
            Size: new Size(width, height),
            ImageRectangle: new Rectangle(left, top, imageWidth, imageHeight)
        );
    }

    private static ImageResult GetCropResult(Size originalSize, int width, int height)
    {
        var cropSides = 0;
        var cropTopBottom = 0;

        if ((float)width / originalSize.Width < (float)height / originalSize.Height) // crop sides
            cropSides = originalSize.Width - (int)Math.Round((float)originalSize.Height / height * width);
        else
            cropTopBottom = originalSize.Height - (int)Math.Round((float)originalSize.Width / width * height);

        return new ImageResult(
            Size: new Size(width, height),
            ImageRectangle: new Rectangle(0, 0, originalSize.Width, originalSize.Height),
            CropRectangle: new Rectangle
            {
                X = cropSides / 2,
                Y = cropTopBottom / 2,
                Width = originalSize.Width - cropSides + cropSides / 2,
                Height = originalSize.Height - cropTopBottom + cropTopBottom / 2
            }
        );
    }

    public string GetCacheName(string filePath)
    {
        var bytes = Encoding.UTF8.GetByteCount(filePath);
        Span<byte> buffer = stackalloc byte[bytes];

        Encoding.UTF8.GetBytes(filePath, buffer);

        var hash = SHA256.HashData(buffer);
        var base64 = Convert.ToBase64String(hash).Replace('/', '_').Replace('+', '-').TrimEnd('=');

        return $"{base64}_{Path.GetFileNameWithoutExtension(filePath).Trim('_').Replace('_', '-')}_w{Width}_h{Height}_m{Mode}_q{Quality}{Format}";
    }

    public static ResizeParams FromQuery(string path, IQueryCollection query)
    {
        var resizeModeStr = query["mode"].ToString().ToLowerInvariant();
        var resizeMode = resizeModeStr switch
        {
            "crop" => ResizeMode.Crop,
            null or "" or "max" => ResizeMode.Max,
            "pad" => ResizeMode.Pad,
            _ => throw new NotSupportedException($"The mode {resizeModeStr} is not supported.")
        };

        var extensionStr = Path.GetExtension(path).ToLowerInvariant();
        var format = extensionStr switch
        {
            ".png" => ImageFormat.Png,
            ".jpeg" or ".jpg" => ImageFormat.Jpeg,
            ".webp" => ImageFormat.Webp,
            _ => throw new NotSupportedException($"The image extension {extensionStr} is not supported.")
        };

        return new ResizeParams
        {
            Quality = (byte)GetInt("quality", defaultValue: 75),
            Width = GetInt("width", defaultValue: 0),
            Height = GetInt("height", defaultValue: 0),
            Mode = resizeMode,
            Format = format
        };

        int GetInt(string key, int defaultValue)
        {
            return query.ContainsKey(key) && int.TryParse(query[key], out var value) ? value : defaultValue;
        }
    }
}
