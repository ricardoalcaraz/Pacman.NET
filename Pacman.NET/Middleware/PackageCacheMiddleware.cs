using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Yarp.ReverseProxy.Model;

namespace Pacman.NET.Middleware;


public class PackageCacheMiddleware : IMiddleware
{
    private readonly RepositoryOptions _cacheOptions;
    private readonly ILogger<PackageCacheMiddleware> _logger;
    private readonly IWebHostEnvironment _env;


    public PackageCacheMiddleware(ILogger<PackageCacheMiddleware> logger,
        IOptions<RepositoryOptions> cacheOptions, 
        IWebHostEnvironment env)
    {
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
        _env = env;
    }

    //
    // /// <summary>
    // ///     Invoke the middleware.
    // /// </summary>
    // /// <param name="context">The <see cref="HttpContext" />.</param>
    // /// <returns>A task that represents the execution of this middleware.</returns>
    // public async Task Invoke(HttpContext ctx)
    // {
    //     var path = ctx.Request.Path;
    //     if (path.StartsWithSegments(_cacheOptions.BaseAddress, out var relativePath))
    //     {
    //         var endpoint = ctx.GetEndpoint();
    //         //TODO: avoid doing work if custom repo is called
    //         if (endpoint is not null)
    //         {
    //             var uri = new Uri(relativePath);
    //             
    //             //create file for proxied request
    //             var fileName = uri.Segments.Last();
    //             var fileInfo = _fileProvider.GetFileInfo(fileName);
    //             var isDb = fileName.EndsWith(".db");
    //             var isSig = fileName.EndsWith(".sig");
    //             
    //             if (isSig)
    //             {
    //                 return;
    //             }
    //             
    //             //stream downloaded file to response body
    //             if (!fileInfo.Exists || isDb || fileInfo.Length <= 1)
    //             {
    //                 _logger.LogDebug("Proxying request for {Name}", fileInfo.Name);
    //                 var fileName = Path.GetRandomFileName();
    //
    //                 var savePath = Path.Combine(_cacheOptions.SaveDirectory, fileName);
    //                 var filePkgInfo = new FileInfo(savePath);
    //                 if(filePkgInfo.Exists)
    //                 {
    //                     await using var fileStream = filePkgInfo.OpenRead();
    //                     await fileStream.CopyToAsync(ctx.Response.Body);
    //                     return;
    //                 }
    //                 await using var fileStream = new FileStream($"{_cacheOptions.CacheDirectory}/{fileName}", FileMode.OpenOrCreate);
    //                 await DownloadPacmanPackage(ctx, fileStream);
    //             }
    //             else if (fileInfo.Exists)
    //             {
    //                 ctx.Response.ContentType = "application/octet-stream";
    //                 ctx.Response.ContentLength = fileInfo.Length;
    //                 await ctx.Response.StartAsync();
    //                 await ctx.Response.SendFileAsync(fileInfo);
    //             }
    //             if (isDb)
    //             {
    //                 return;
    //             }
    //             
    //             return;
    //         }
    //
    //         ctx.Response.StatusCode = 404;
    //         try
    //         {
    //             //var packageStream = await _pacmanService.GetPackageStream(path, ctx.RequestAborted);
    //             return;
    //         }
    //         catch (HttpRequestException)
    //         {
    //             ctx.Response.StatusCode = 404;
    //         }
    //         catch (IOException)
    //         {
    //         }
    //         catch (Exception)
    //         {
    //             ctx.Response.StatusCode = 500;
    //         }
    //     }
    //
    //     _logger.LogTrace("Skipping pacman cache middleware");
    //     await _next(ctx);
    // }
    
    //
    // public async Task GetRateLimiterAsync(HttpContext context)
    // {
    //     var slidingWindow = new SlidingWindowRateLimiter(_rateLimiterOptions.Value);
    //
    //     var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);
    //     var bufferSize = 1024 * 16;
    //     var bytesRead = 0;
    //     do
    //     {
    //         using var lease = await slidingWindow.AcquireAsync(permitCount);
    //         var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
    //         
    //         bytesRead = await packageStream.ReadAsync(buffer, context.RequestAborted);
    //         var dataReceived = buffer.AsMemory(0, bytesRead);
    //
    //         await fileStream.WriteAsync(dataReceived, context.RequestAborted);
    //         await context.Response.BodyWriter.WriteAsync(dataReceived, context.RequestAborted);
    //         ArrayPool<byte>.Shared.Return(buffer);
    //     } while (bytesRead > 0);
    // }


    public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
    {
        if (ctx.Request.Path.StartsWithSegments(_cacheOptions.BaseAddress, out var pathString))
        {
            var fileName = Path.GetFileName(pathString);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogInformation("No package name given");
                ctx.Response.StatusCode = 404;
                return;
            }

            var packageInfo = _cacheOptions.PackageProvider.GetFileInfo(fileName);
            packageInfo = packageInfo.Exists ? packageInfo : _cacheOptions.RepositoryProvider.GetFileInfo(pathString);
            if (packageInfo.Exists)
            {
                var file = new FileInfo(packageInfo.PhysicalPath!);
                if (string.IsNullOrWhiteSpace(file.LinkTarget))
                {
                    await ctx.Response.SendFileAsync(packageInfo);
                }
                else
                {
                    var fileInfo = File.ResolveLinkTarget(file.LinkTarget, true) as FileInfo;
                    await ctx.Response.SendFileAsync(new PhysicalFileInfo(fileInfo!));
                }
                ctx.Response.ContentType = "application/octet-stream";
                return;
            }

            _logger.LogInformation("File not found in cache for {Name}", fileName);
            var proxyFeature = ctx.Features.Get<IReverseProxyFeature>();

            if (proxyFeature is null)
            {
                _logger.LogWarning("Proxy feature not found");
                ctx.Response.StatusCode = 404;
                ctx.Response.Clear();
                await ctx.Response.CompleteAsync();
                return;
            }

            _logger.LogDebug("Proxy feature found, creating a wrapped stream for {Name}", fileName);

            var tempFileName = Path.GetTempFileName();

            try
            {
                var originalBody = ctx.Features.Get<IHttpResponseBodyFeature>()!;
                await using var pacmanCacheBody = new PacmanPackageStream(originalBody, tempFileName);
                ctx.Features.Set<IHttpResponseBodyFeature>(pacmanCacheBody);
                await next(ctx);
                ctx.Features.Set(originalBody);
                await ctx.Response.CompleteAsync();
            }
            finally
            {
                if (ctx.Response.StatusCode == 200)
                {
                    var tmpFileInfo = new FileInfo(tempFileName);
                    if (tmpFileInfo.Exists)
                    {
                        if (tmpFileInfo.Length == ctx.Response.ContentLength && Path.GetExtension(pathString) != "db")
                        {
                            File.Copy(tempFileName, Path.Combine(_cacheOptions.PackageDirectory, Path.GetFileName(pathString)));
                        }
                    }
                }
            }

            //if response still hasn't started then declare it as a 404
            // if (!ctx.Response.HasStarted)
            // {
            //     _logger.LogDebug("No file found for {Name}", path);
            //     ctx.Response.Clear();
            //     ctx.Response.StatusCode = 404;
            //     await ctx.Response.CompleteAsync();
            // }
            // //it is important that no execute permission should ever be given to new files
            // const UnixFileMode GLOBAL_READ = UnixFileMode.UserRead | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
            // const UnixFileMode FILE_PERM = GLOBAL_READ | UnixFileMode.UserWrite;
            //
            // _logger.LogDebug("{Name} has {Perm} permissions", fileInfo.Name, fileInfo.UnixFileMode);
            //
            // //set file permissions as a precaution to ensure no executable files are being used as a cache
            // fileInfo.UnixFileMode = FILE_PERM;
            // var name = Path.GetTempFileName();
            //
            // if (fileInfo.Exists)
            // {
            //     try
            //     {
            //         
            //     
            //
            //         //after file is down downloading create background process to save tmp file to disk
            //         
            //         //reset stream to beginning
            //     }
            //     catch (IOException)
            //     {
            //
            //     }
            //     catch(UnauthorizedAccessException)
            //     {
            //
            //     }
            // }
            // var tmpFileStreamOptions = new FileStreamOptions
            // {
            //     BufferSize = 4096,
            //     Access = FileAccess.ReadWrite,
            //     Mode = FileMode.Create,
            //     Options = FileOptions.SequentialScan,
            //     PreallocationSize = 4096,
            //     Share = FileShare.None,
            //     UnixCreateMode = FILE_PERM
            // };
            // var tmpFileStream = new FileStream(name, tmpFileStreamOptions);
            //
            // try
            // {
            //     var fileSte = new FileStream(name, FileMode.Create, FileAccess.Read, FileShare.Read);
            //     await using var fileStream = File.OpenWrite(name);
            //     //move package body to ctor
            //     //wrap body stream in a rate limiter
            //     await next(ctx);
            //     
            //     //file cleanup
            //     
            // }
            // catch (IOException ex)
            // {
            //     _logger.LogWarning("Permission denied to write {File} to {Path}", name, _cacheOptions.SaveDirectory);
            // }
            //
            // await next(ctx);
            //
            // await ctx.Response.StartAsync();
            //
            // var packageSize = ctx.Response.ContentLength ?? 0;
            // _logger.LogInformation("File Size of {Name} is {Size}", fileName, packageSize);
            //
            // //TODO: catch exceptions when completing
            // await ctx.Response.CompleteAsync();
            //
            // tmpFileStream.Position = tmpFileStream.Seek(0, SeekOrigin.Begin);
            //
            // if (tmpFileStream.Position == packageSize)
            // {
            //     _logger.LogInformation("File {Name} has been downloaded", fileInfo.Name);
            //     
            //     
            //     try
            //     {
            //         //move tmp file to cache directory
            //         await _persistentFileService.EnqueueRequest(new PackageCacheRequest
            //         {
            //             PackageStream = tmpFileStream,
            //             PackageName = fileInfo.Name
            //         });
            //         _logger.LogDebug("Successfully enqueued {Name} for cache persistence", fileInfo.Name);
            //     }
            //     catch (Exception ex)
            //     {
            //         _logger.LogError(ex, "Error occured while trying to persist {Name}", fileName);
            //         await tmpFileStream.DisposeAsync();
            //     }
            // }
            //
            //
        }

    }
    
    private IFileProvider CreateFileProvider(string name)
    {
        var fileProviderPath = _env.IsDevelopment()
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : _env.ContentRootPath;

        var directoryInfo = Directory.CreateDirectory(Path.Combine(fileProviderPath, name));
        if (directoryInfo.Exists)
        {
            _logger.LogInformation("Found directory {Name}", directoryInfo);
        }

        return new PhysicalFileProvider(directoryInfo.FullName);
    }
}

public record PackageCacheMiddlewareOptions
{
    public required IFileProvider PackageProvider { get; set; }
    public required IFileProvider RepositoryProvider { get; set; }
}