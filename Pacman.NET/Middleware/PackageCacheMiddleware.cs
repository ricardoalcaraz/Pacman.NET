using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Yarp.ReverseProxy.Model;

namespace Pacman.NET.Middleware;


public class PackageCacheMiddleware : IMiddleware
{
    private const string OCTET_STREAM = "application/octet-stream";
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


    public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
    {
        if (ctx.Request.Path.StartsWithSegments(_cacheOptions.BaseAddress, out var pathString))
        {
            var fileName = Path.GetFileName(pathString);
            var packageInfo = _cacheOptions.PackageProvider.GetFileInfo(fileName);
            packageInfo = packageInfo.Exists ? packageInfo : _cacheOptions.RepositoryProvider.GetFileInfo(pathString);
            
            bool isInvalidFile = string.IsNullOrWhiteSpace(fileName) || !packageInfo.Exists;
            if (isInvalidFile)
            {
                _logger.LogInformation("No file found for {Name}", fileName);
                ctx.Response.StatusCode = 404;
                await next(ctx);
                return;
            }

            if (packageInfo.Exists)
            {
                await SendFileAsync(ctx, packageInfo);
            }

            if (DateTime.TryParse(ctx.Request.Headers.LastModified, out var lastModified))
            {
                if (packageInfo.LastModified >= lastModified)
                {
                    _logger.LogInformation("New file found, returning from cache");
                }
            }
            _logger.LogInformation("File not found in cache for {Name}", fileName);
            var proxyFeature = ctx.Features.Get<IReverseProxyFeature>();

            if (proxyFeature is null)
            {
                _logger.LogWarning("Proxy feature not found");
                ctx.Response.Clear();
                ctx.Response.StatusCode = 404;

                await ctx.Response.CompleteAsync();
                return;
            }

            await ProxyRequest(ctx, next, fileName, pathString);
        }

    }

    private static async Task SendFileAsync(HttpContext ctx, IFileInfo packageInfo)
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
        ctx.Response.ContentType = OCTET_STREAM;
    }

    private async Task ProxyRequest(HttpContext ctx, RequestDelegate next, string fileName, PathString pathString)
    {
        _logger.LogDebug("Proxy feature found, creating a wrapped stream for {Name}", fileName);

        string tempFileName = Path.GetTempFileName();

        try
        {
            //response body is read only so we'll switch it so we can cache the body as it's getting streamed
            var originalBody = ctx.Features.Get<IHttpResponseBodyFeature>()!;
            var tmpFileStream = new FileStream(tempFileName, new FileStreamOptions
            {
                Access = FileAccess.Write,
                Mode = FileMode.Create,
                Options = FileOptions.SequentialScan,
                Share = FileShare.None
            });
            await using var cachingStream = new CachingStream(originalBody, tmpFileStream);
            ctx.Features.Set<IHttpResponseBodyFeature>(cachingStream);

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
    }
}

public record PackageCacheMiddlewareOptions
{
    public required IFileProvider PackageProvider { get; set; }
    public required IFileProvider RepositoryProvider { get; set; }
}