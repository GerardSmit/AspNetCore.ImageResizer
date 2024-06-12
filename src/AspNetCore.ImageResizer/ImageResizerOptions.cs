using System.Drawing;

namespace GerardSmit.AspNetCore.ImageResizer;

public class ImageResizerOptions
{
    public HashSet<Size> AllowedSizes { get; set; } = new();

    public HashSet<byte> AllowedQualities { get; set; } = new();

    public string CacheFolder { get; set; } = "temp/image_resizer";
}
