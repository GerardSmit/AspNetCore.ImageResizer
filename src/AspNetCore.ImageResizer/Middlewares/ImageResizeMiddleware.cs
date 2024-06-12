using GerardSmit.AspNetCore.ImageResizer.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace GerardSmit.AspNetCore.ImageResizer.Middlewares;

public class ImageResizeMiddleware : IMiddleware
{
    private readonly IWebHostEnvironment _environment;
    private readonly string _tempFolder;
    private readonly IOptions<ImageResizerOptions> _options;

    public ImageResizeMiddleware(IWebHostEnvironment environment, IOptions<ImageResizerOptions>? options = null)
    {
        _environment = environment;
        _options = options ?? new OptionsWrapper<ImageResizerOptions>(new ImageResizerOptions());
        _tempFolder = Path.Combine(environment.ContentRootPath, _options.Value.CacheFolder);

        Directory.CreateDirectory(_tempFolder);
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path.Value;

        if (path == null)
        {
            await next(context);
            return;
        }

        var lastIndex = path.LastIndexOf('.');

        if (lastIndex == -1 || path.AsSpan(lastIndex) is not ".ir")
        {
            await next(context);
            return;
        }

        var offset = path[0] == '/' ? 1 : 0;
        var info = _environment.WebRootFileProvider.GetFileInfo(path.Substring(offset, lastIndex - offset));

        if (!info.Exists)
        {
            await next(context);
            return;
        }

        var resizeParams = ResizeParams.FromQuery(info.Name, context.Request.Query);

        if (_options.Value.AllowedSizes is { Count: > 0 } allowedSizes && !allowedSizes.Contains(resizeParams.Size))
        {
            await next(context);
            return;
        }

        if (_options.Value.AllowedQualities is { Count: > 0 } allowedQualities && !allowedQualities.Contains(resizeParams.Quality))
        {
            await next(context);
            return;
        }

        if (context.Request.Headers.Accept.ToString().Contains("image/webp", StringComparison.OrdinalIgnoreCase))
        {
            resizeParams = resizeParams with { Format = ImageFormat.Webp };
        }

        // Check cache.
        var cachePath = Path.Combine(_tempFolder, resizeParams.GetCacheName(info.PhysicalPath ?? path));

        if (!File.Exists(cachePath))
        {
            var imageResizer = context.RequestServices.GetRequiredService<IImageResizer>();

            await CreateResizedImageAsync(imageResizer, info, cachePath, resizeParams);
        }

        await using var stream = File.Open(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        PrepareResponse(context.Response);

        context.Response.ContentType = resizeParams.Format.GetMimeType();
        context.Response.ContentLength = stream.Length;

        await stream.CopyToAsync(context.Response.Body);
    }

    private async Task CreateResizedImageAsync(IImageResizer imageResizer, IFileInfo imagePath, string cachePath, ResizeParams resizeParams)
    {
        // Create resized file.
        await using var stream = imagePath.CreateReadStream();

        // Create cache file.
        var tempFile = cachePath + ".temp";

        if (!File.Exists(tempFile))
        {
            try
            {
                await using (var fileStream = File.Open(tempFile, FileMode.CreateNew))
                {
                    await imageResizer.ResizeAsync(stream, fileStream, resizeParams);
                }

                File.Move(tempFile, cachePath);
            }
            catch
            {
                // ignore.
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
                catch
                {
                    // ignore.
                }
            }
        }
    }

    private static void PrepareResponse(HttpResponse response)
    {
        var typedHeaders = response.GetTypedHeaders();

        // TODO Set E-tag.

        typedHeaders.CacheControl = new CacheControlHeaderValue()
        {
            Public = true,
            MaxAge = TimeSpan.FromDays(7)
        };

        response.Headers[HeaderNames.Vary] = new[] { "Accept-Encoding" };
    }
}