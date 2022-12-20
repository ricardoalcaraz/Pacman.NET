using System.Buffers;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;

namespace Pacman.NET.Middleware;

public class PackageCacheMiddleware : IMiddleware
{
    private readonly IOptions<PacmanOptions> _cacheOptions;
    private readonly string[] _excludedFileTypes;
    private readonly ILogger<PackageCacheMiddleware> _logger;
    private readonly IPacmanService _pacmanService;
    private readonly IMemoryCache _memoryCache;
    private readonly PhysicalFileProvider _fileProvider;


    public PackageCacheMiddleware(IOptions<PacmanOptions> cacheOptions,
        ILogger<PackageCacheMiddleware> logger,
        IPacmanService pacmanService,
        IMemoryCache memoryCache)
    {
        _cacheOptions = cacheOptions;
        _logger = logger;
        _pacmanService = pacmanService;
        _memoryCache = memoryCache;
        _fileProvider = new PhysicalFileProvider(cacheOptions.Value.CacheDirectory);
        _excludedFileTypes = new[] { "db", "db.sig", "files" };
    }


    /// <summary>
    ///     Invoke the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext" />.</param>
    /// <returns>A task that represents the execution of this middleware.</returns>
    public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
    {
        var options = _cacheOptions.Value;

        var path = ctx.Request.Path;

        if (path.StartsWithSegments(options.BaseAddress, out var relativePath))
        {
            var endpoint = ctx.GetEndpoint();
            //TODO: avoid doing work if custom repo is called
            if (endpoint is not null)
            {
                var uri = new Uri(relativePath);
                var fileName = uri.Segments.Last();
                var excludedFileType = _excludedFileTypes.SingleOrDefault(e => fileName.EndsWith(e));

                if (excludedFileType is not null)
                {
                    _logger.LogDebug("Excluded file {Type} will be proxied", excludedFileType);
                    await next(ctx);
                    return;
                }
                
                var fileInfo = _fileProvider.GetFileInfo(fileName);
                
                ctx.Response.Body = fileInfo switch
                {
                    {Exists: true} when excludedFileType is null => fileInfo.CreateReadStream(),
                    {Exists: false} => await ProxyStream(excludedFileType!),
                    _ => Stream.Null
                };
                
                ctx.Response.ContentLength = 0;
                ctx.Response.ContentType = "application/octet-stream";

                //stream downloaded file to response body
                if (!fileInfo.Exists || fileInfo.Length <= 1)
                {
                    _logger.LogDebug("Proxying request for {Name}", fileInfo.Name);
                    await using var fileStream = new FileStream($"{options.CacheDirectory}/{fileName}", FileMode.OpenOrCreate);
                    await DownloadPacmanPackage(ctx, fileStream);
                }

                return;
            }

            ctx.Response.StatusCode = 404;
            try
            {
                //var packageStream = await _pacmanService.GetPackageStream(path, ctx.RequestAborted);
                return;
            }
            catch (HttpRequestException)
            {
                ctx.Response.StatusCode = 404;
            }
            catch (IOException)
            {
            }
            catch (Exception)
            {
                ctx.Response.StatusCode = 500;
            }
        }

        _logger.LogTrace("Skipping pacman cache middleware");
        await next(ctx);
    }

    private async Task SetHeaders(HttpContext ctx)
    {
        if (!ctx.Response.HasStarted)
        {
            ctx.Response.ContentType = "application/octet-stream";
            await ctx.Response.StartAsync();
        }
    }

    private Task<Stream> ProxyStream(string fileName)
    {
        return Task.FromResult(Stream.Null);
    }

    public async Task GetRateLimiterAsync(HttpContext context)
    {
        var enableRateLimitingAttribute = context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>();
        var rateLimiterOptions = new SlidingWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = 6,
            QueueLimit = 6,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            SegmentsPerWindow = 5,
            Window = TimeSpan.FromMinutes(1)
        };
        var slidingWindow = new SlidingWindowRateLimiter(rateLimiterOptions);

        //represents the total size of the file
        var permitCount = 25;

        var packageStream = await _pacmanService.GetPackageStream(context.Request.Path, context.RequestAborted);

        var tempFileName = Path.GetTempFileName();
        await using var fileStream = File.OpenWrite(tempFileName);
        await SetHeaders(context);

        var bufferSize = 1024 * 16;
        var bytesRead = 0;
        do
        {
            using var lease = await slidingWindow.AcquireAsync(permitCount);
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            bytesRead = await packageStream.ReadAsync(buffer, context.RequestAborted);
            var dataReceived = buffer.AsMemory(0, bytesRead);

            await fileStream.WriteAsync(dataReceived, context.RequestAborted);
            await context.Response.BodyWriter.WriteAsync(dataReceived, context.RequestAborted);
            ArrayPool<byte>.Shared.Return(buffer);
        } while (bytesRead > 0);
    }


    public Task DownloadPacmanPackage(HttpContext context, FileStream cacheStream)
    {
        var originalBody = context.Features.Get<IHttpResponseBodyFeature>()!;
        var logger = context.RequestServices.GetRequiredService<ILogger<PacmanPackageBody>>();
        var httpResponseBodyFeature = new PacmanPackageBody(originalBody, cacheStream, logger);
        context.Features.Set<IHttpResponseBodyFeature>(httpResponseBodyFeature);

        return Task.CompletedTask;
    }
}