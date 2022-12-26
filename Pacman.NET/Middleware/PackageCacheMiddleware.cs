using System.Buffers;
using System.IO.Pipelines;
using System.Security.AccessControl;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;

namespace Pacman.NET.Middleware;


public class PackageCacheMiddleware : IMiddleware
{
    private static readonly string[] _excludedFileTypes = { "db", "db.sig", "files"};
    private readonly PacmanOptions _cacheOptions;
    private readonly ILogger<PackageCacheMiddleware> _logger;
    private readonly IPacmanService _pacmanService;
    private readonly IFileProvider _fileProvider;


    public PackageCacheMiddleware(ILogger<PackageCacheMiddleware> logger,
        IOptions<PacmanOptions> cacheOptions, 
        IPacmanService pacmanService)
    {
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
        _pacmanService = pacmanService;
        _fileProvider = new PhysicalFileProvider(cacheOptions.Value.CacheDirectory);
    }


    /// <summary>
    ///     Invoke the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext" />.</param>
    /// <returns>A task that represents the execution of this middleware.</returns>
    public async Task Invoke(HttpContext ctx)
    {
        var path = ctx.Request.Path;
        if (path.StartsWithSegments(_cacheOptions.BaseAddress, out var relativePath))
        {
            var endpoint = ctx.GetEndpoint();
            //TODO: avoid doing work if custom repo is called
            if (endpoint is not null)
            {
                var uri = new Uri(relativePath);
                var fileName = uri.Segments.Last();
                var fileInfo = _fileProvider.GetFileInfo(fileName);
                var isDb = fileName.EndsWith(".db");
                var isSig = fileName.EndsWith(".sig");
                
                if (isSig)
                {
                    return;
                }
                
                //stream downloaded file to response body
                if (!fileInfo.Exists || isDb || fileInfo.Length <= 1)
                {
                    _logger.LogDebug("Proxying request for {Name}", fileInfo.Name);
                    var fileName = Path.GetRandomFileName();

                    var savePath = Path.Combine(_cacheOptions.SaveDirectory, fileName);
                    var filePkgInfo = new FileInfo(savePath);
                    if(filePkgInfo.Exists)
                    {
                        await using var fileStream = filePkgInfo.OpenRead();
                        await fileStream.CopyToAsync(ctx.Response.Body);
                        return;
                    }
                    await using var fileStream = new FileStream($"{_cacheOptions.CacheDirectory}/{fileName}", FileMode.OpenOrCreate);
                    await DownloadPacmanPackage(ctx, fileStream);
                }
                else if (fileInfo.Exists)
                {
                    ctx.Response.ContentType = "application/octet-stream";
                    ctx.Response.ContentLength = fileInfo.Length;
                    await ctx.Response.StartAsync();
                    await ctx.Response.SendFileAsync(fileInfo);
                }
                if (isDb)
                {
                    return;
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
        await _next(ctx);
    }


    private async Task SetHeaders(HttpContext ctx)
    {
        if (!ctx.Response.HasStarted)
        {
            ctx.Response.ContentType = "application/octet-stream";
            await ctx.Response.StartAsync();
        }
    }


    public async Task GetRateLimiterAsync(HttpContext context)
    {
        var enableRateLimitingAttribute = context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>();

        var slidingWindow = new SlidingWindowRateLimiter(_rateLimiterOptions.Value);

        await SetHeaders(context);
        var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);
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


    public async Task DownloadPacmanPackage(HttpContext context, FileStream cacheStream)
    {
        var originalBody = context.Features.Get<IHttpResponseBodyFeature>()!;
        var body = new PacmanPackageBody(originalBody, cacheStream);
        context.Features.Set<IHttpResponseBodyFeature>(body);
        context.Response.ContentType = "application/octet-stream";
     
        await _next(context);
        //context.Features.Set(originalBody);
    }


    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        //represents the total size of the file
        var fileName = Path.GetFileName(context.Request.Path);
        
        var fileInfo = new FileInfo(fileName);
        
        //it is important that no execute permission should ever be given to new files
        const UnixFileMode GLOBAL_READ = UnixFileMode.UserRead | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
        const UnixFileMode FILE_PERM = GLOBAL_READ | UnixFileMode.UserWrite;

        _logger.LogDebug("{Name} has {Perm} permissions", fileInfo.Name, fileInfo.UnixFileMode);
        
        //set file permissions as a precaution to ensure no executable files are being used as a cache
        fileInfo.UnixFileMode = FILE_PERM;
        var name = Path.GetTempFileName();
        if (fileInfo.Exists)
        {
            try
            {
                var tmpFileStreamOptions = new FileStreamOptions
                {
                    BufferSize = 4096,
                    Access = FileAccess.Read,
                    Mode = FileMode.Create,
                    Options = FileOptions.DeleteOnClose,
                    PreallocationSize = 4096,
                    Share = FileShare.Read,
                    UnixCreateMode = FILE_PERM
                };
            
                var tmpFileStream = new FileStream(name, tmpFileStreamOptions);

                //remove execute permissions on file 
                fileInfo.UnixFileMode &= ~(UnixFileMode.UserExecute | UnixFileMode.OtherExecute | UnixFileMode.GroupExecute);
                
                //after file is down downloading create background process to save tmp file to disk
                
            }
            catch (IOException)
            {

            }
            catch(UnauthorizedAccessException)
            {

            }
        }
        else
        {
            
        }
        
        var tempFileName = name;
        try
        {
            var tmpFile = name;
            await using var tmpFileStream = new FileStream(tmpFile, FileMode.Create, FileAccess.Read, FileShare.None);
            var fileSte = new FileStream(tempFileName, FileMode.Create, FileAccess.Read, FileShare.Read);
            await using var fileStream = File.OpenWrite(tempFileName);
            
            //wrap body stream in a rate limiter
            await next(context);
            
            //file cleanup
            
        }
        catch (IOException ex)
        {
            _logger.LogWarning("Permission denied to write {File} to {Path}", tempFileName, _cacheOptions.SaveDirectory);
        }

        await next(context);
    }
}