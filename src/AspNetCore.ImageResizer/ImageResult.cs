using System.Drawing;

namespace GerardSmit.AspNetCore.ImageResizer;

public record ImageResult(
    Size Size,
    Rectangle ImageRectangle,
    Rectangle? CropRectangle = null
);