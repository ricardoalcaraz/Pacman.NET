using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;

namespace Pacman.NET.Middleware;

public class PackageCacheMiddleware
{
    private readonly IOptions<PacmanOptions> _cacheOptions;
    private readonly string[] _excludedFileTypes = { "db", "db.sig", "files" };
    private readonly ILogger<PackageCacheMiddleware> _logger;
    private readonly RequestDelegate _next;
    private readonly IPacmanService _pacmanService;
    private readonly IMemoryCache _memoryCache;
    private readonly IOptions<SlidingWindowRateLimiterOptions> options;
    private PhysicalFileProvider _fileProvider;


    public PackageCacheMiddleware(RequestDelegate next,
                                  IOptions<PacmanOptions> cacheOptions,
                                  ILogger<PackageCacheMiddleware> logger,
                                  IPacmanService pacmanService, 
                                  IMemoryCache memoryCache)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _cacheOptions = cacheOptions;
        _logger = logger;
        _pacmanService = pacmanService;
        _memoryCache = memoryCache;
        _fileProvider = new PhysicalFileProvider(cacheOptions.Value.CacheDirectory);
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

        if (path.StartsWithSegments(options.BaseAddress, out var relativePath))
        {
            var endpoint = ctx.GetEndpoint();
            if (endpoint is not null)
            {
                var uri = new Uri(relativePath);
                var fileName = uri.Segments.Last();
                var fileInfo = _fileProvider.GetFileInfo(fileName);
                var isDb = fileName.EndsWith(".db");
                var isSig = fileName.EndsWith(".sig");
                if (isSig)
                {
                    await _next(ctx);
                    return;
                }
                if (!fileInfo.Exists || isDb)
                {
                    _logger.LogWarning("No cache file found for {Name}, proxying request", fileInfo.Name);
                    
                    
                        var tempFile = await DownloadPacmanPackage(ctx, ctx.RequestAborted);
                        if (ctx.Response.StatusCode == 200)
                        {
                            await ctx.Response.StartAsync();
                            var fileStream = new FileStream($"{options.CacheDirectory}/{fileName}", FileMode.OpenOrCreate);
                            var responseStream = tempFile.OpenRead();

                            var bufferSize = 1024 * 16;
                            var bytesRead = 0;
                            do
                            {
                                var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                                bytesRead = await responseStream.ReadAsync(buffer, ctx.RequestAborted);
                                var dataReceived = buffer.AsMemory(0, bytesRead);

                                await fileStream.WriteAsync(dataReceived, ctx.RequestAborted);
                                await ctx.Response.BodyWriter.WriteAsync(dataReceived, ctx.RequestAborted);
                                ArrayPool<byte>.Shared.Return(buffer);
                            } while (bytesRead > 0);
                        }
                }
                //await ctx.Response.SendFileAsync(fileInfo);

                if (isDb)
                {
                    return;
                }
                
                return;
            }

            try
            {
                //var packageStream = await _pacmanService.GetPackageStream(path, ctx.RequestAborted);
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
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "application/octet-stream";
        }
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


    public async Task<FileInfo> DownloadPacmanPackage(HttpContext context, CancellationToken ctx)
    {
        var tempFileName = Path.GetTempFileName();
        await using var tempFileStream = File.OpenWrite(tempFileName);
        var originalBody = context.Features.Get<IHttpResponseBodyFeature>();
        var body = new StreamResponseBodyFeature(tempFileStream, originalBody);
        context.Features.Set<IHttpResponseBodyFeature>(body);
                    
        await _next(context);
        var statusCode = context.Response.StatusCode;
        await tempFileStream.FlushAsync(ctx);
        var contentType = context.Response.ContentType;
        context.Features.Set(body.PriorFeature);

        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/octet-stream";
            context.Response.ContentLength = tempFileStream.Length;
        }

        return new FileInfo(tempFileName);
    }
    
    
}