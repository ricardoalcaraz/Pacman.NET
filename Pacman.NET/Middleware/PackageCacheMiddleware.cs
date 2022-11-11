using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Pacman.NET.Middleware;

public class PackageCacheMiddleware
{
    private readonly IOptions<PackageCacheOptions> _cacheOptions;
    private readonly string[] _excludedFileTypes = { "db", "db.sig", "files" };
    private readonly ILogger<PackageCacheMiddleware> _logger;
    private readonly RequestDelegate _next;
    private readonly IPacmanService _pacmanService;
    private readonly IOptions<SlidingWindowRateLimiterOptions> options;


    public PackageCacheMiddleware(RequestDelegate next,
                                  IOptions<PackageCacheOptions> cacheOptions,
                                  ILogger<PackageCacheMiddleware> logger,
                                  IPacmanService pacmanService)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _cacheOptions = cacheOptions;
        _logger = logger;
        _pacmanService = pacmanService;
    }


    /// <summary>
    ///     Invoke the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext" />.</param>
    /// <returns>A task that represents the execution of this middleware.</returns>
    public async Task Invoke(HttpContext ctx)
    {
        var options = _cacheOptions.Value;
        var path = ctx.Request.Path;

        //await _next(ctx);

        if (path.StartsWithSegments("/archlinux", out var relativePath))
        {
            var endpoint = ctx.GetEndpoint();
            if (endpoint is not null)
            {
                await _next(ctx);
                return;
            }

            var fileInfo = options.FileProvider.GetFileInfo(relativePath);

            if (fileInfo.Exists)
            {
                SetHeaders(ctx);
                await ctx.Response.SendFileAsync(fileInfo);
                return;
            }

            if (string.IsNullOrWhiteSpace(fileInfo.Name))
            {
                await _next(ctx);
                return;
            }

            try
            {
                var packageStream = await _pacmanService.GetPackageStream(path, ctx.RequestAborted);
                //await WriteResponseAsync(ctx, tempFileName, packageStream);
                //await ctx.Response.SendFileAsync(tempFileName);
                //File.Move(tempFileName, fileInfo.PhysicalPath!, true);
                return;
            }
            catch (HttpRequestException ex)
            {
                ctx.Response.StatusCode = 404;
            }
            catch (IOException ex)
            {
            }
            catch (Exception ex)
            {
                ctx.Response.StatusCode = 500;
            }
        }

        _logger.LogTrace("Skipping pacman cache middleware");


        await _next(ctx);
    }


    private void SetHeaders(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.ContentType = "application/octet-stream";
    }


    public async Task GetRateLimiterAsync(HttpContext context)
    {
        var enableRateLimitingAttribute = context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>();

        var slidingWindow = new SlidingWindowRateLimiter(options.Value);

        //represents the total size of the file
        var permitCount = 25;

        var packageStream = await _pacmanService.GetPackageStream(context.Request.Path, context.RequestAborted);

        var tempFileName = Path.GetTempFileName();
        await using var fileStream = File.OpenWrite(tempFileName);
        SetHeaders(context);

        var bufferSize = 1024 * 16;
        var totalBytesProcessed = 0;
        var numBytesProcessed = 0;
        var bytesRead = 0;
        do
        {
            using var lease = await slidingWindow.AcquireAsync(permitCount);
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            bytesRead = await packageStream.ReadAsync(buffer, context.RequestAborted);
            var dataReceived = buffer.AsMemory(0, bytesRead);
            numBytesProcessed += bytesRead;

            await fileStream.WriteAsync(dataReceived, context.RequestAborted);
            await context.Response.BodyWriter.WriteAsync(dataReceived, context.RequestAborted);
            ArrayPool<byte>.Shared.Return(buffer);
        } while (bytesRead > 0);
    }


    public async Task<FileInfo> DownloadPacmanPackage(string path, PipeWriter writer, CancellationToken ctx)
    {
        return new FileInfo(path);
    }
}