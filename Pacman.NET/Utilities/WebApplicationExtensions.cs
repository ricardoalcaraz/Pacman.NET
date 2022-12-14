using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Pacman.NET.Middleware;
using Yarp.ReverseProxy.Model;

namespace Pacman.NET.Utilities;

public static class WebApplicationExtensions
{
    public static WebApplicationBuilder AddPacmanServer(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<PackageCacheMiddleware>();
        builder.Services.AddSingleton<MirrorSyncService>();
        builder.Services.AddSingleton<PacmanService>();
        builder.Services.AddSingleton<PersistentFileService>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<PersistentFileService>());
        builder.Services.AddSingleton<IPacmanService, PacmanService>(sp => sp.GetRequiredService<PacmanService>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<PacmanService>());
        builder.Services.AddHostedService<MirrorSyncService>(sp => sp.GetRequiredService<MirrorSyncService>());
        builder.Services.AddHttpClient<MirrorClient>();
        builder.Services.AddSingleton<PacmanConfigParser>();
        builder.Services.AddScoped<PackageCacheMiddleware>();
        var mirrorConfigSetting = builder.Configuration["Pacman:MirrorUrl"] ?? throw new InvalidOperationException("MirrorUrl is a required config setting");
        var mirrorUri = new Uri(mirrorConfigSetting, UriKind.Absolute);
        if (mirrorUri.IsFile)
        {
            builder.Services.AddTransient<IMirrorService, MirrorListParseService>();
        }
        else
        {
            builder.Services.AddTransient<IMirrorService, RemoteMirrorService>();
        }
        //load an empty array to add the in memory IProxyConfigProvider class so we can update it later
        builder.Services
            .AddReverseProxy()
            .LoadFromMemory(Array.Empty<RouteConfig>(), Array.Empty<ClusterConfig>());
        
        builder.Services.AddOptions<PacmanOptions>()
            .BindConfiguration("Pacman")
            .Configure(opt =>
            {
                if(builder.Environment.IsDevelopment())
                {
                    var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    opt.SaveDirectory = Path.Combine(homeDirectory, ".cache", "pacnet");
                }
            })
            .PostConfigure(opt =>
            {
                Directory.CreateDirectory(opt.SaveDirectory);
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        builder.Services.AddMemoryCache();
        builder.Services.AddOptions<ApplicationOptions>()
            .BindConfiguration("PacmanConfig")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        builder.Services.AddHttpClient();
        return builder;
    }
    
    public static WebApplication UsePacmanCache(this WebApplication app)
    {
        var pacmanOptions = app.Services.GetRequiredService<IOptions<PacmanOptions>>().Value;

        
        
        // app.UseFileServer(new FileServerOptions
        // {
        //     RequestPath = pacmanOptions.BaseAddress,
        //     FileProvider = compositeProvider,
        //     EnableDefaultFiles = false,
        //     RedirectToAppendTrailingSlash = true,
        //     EnableDirectoryBrowsing = true,
        //     StaticFileOptions =
        //     {
        //         DefaultContentType = "application/octet-stream",
        //         ServeUnknownFileTypes = true
        //     }
        // });
        
        app.UseMiddleware<PackageCacheMiddleware>();

        return app;
    }

    public static IReverseProxyApplicationBuilder UsePackageCache(this IReverseProxyApplicationBuilder proxyBuilder)
    {
        proxyBuilder.Use(async (ctx, next) =>
        {
            var originalBody = ctx.Features.Get<IHttpResponseBodyFeature>()!;
            var pacmanCacheBody = new PacmanPackageStream(originalBody);            
            ctx.Features.Set<IHttpResponseBodyFeature>(pacmanCacheBody);
            
            var proxyFeature = ctx.GetReverseProxyFeature();
            var logger = ctx.RequestServices.GetRequiredService<ILogger<IReverseProxyFeature>>();

            try
            {
                await next(ctx);
            
            }
            finally
            {
                ctx.Features.Set(originalBody);
            }

            var ifModifiedSince = ctx.Request.Headers.IfModifiedSince.LastOrDefault();
            if (DateTime.TryParse(ifModifiedSince, out var dateTime))
            {
                logger.LogInformation("Modified since header : {Time}", dateTime.ToLocalTime());
            }

            logger.LogInformation("Proxied request code {Status}", ctx.Response.StatusCode);
          
            var errorFeature = ctx.GetForwarderErrorFeature();
            var lengthBefore = ctx.Response.ContentLength;
            logger.LogInformation("Started: {State}, Size: {Size2}", ctx.Response.HasStarted, lengthBefore);

            if (ctx.Response is { HasStarted: true, StatusCode: 200 })
            { 
                logger.LogInformation("Response has started for {Name}", ctx.Request.Path);
                var fileService = ctx.RequestServices.GetRequiredService<PersistentFileService>();
                var cachedFile = pacmanCacheBody.GetTempFile();
                
                logger.LogInformation("Sending {File} to get saved", cachedFile.Name);
                await fileService.EnqueueRequest(new PackageCacheRequest
                {
                    PackageName = Path.GetFileName(ctx.Request.Path),
                    PackageStream = cachedFile
                });
                return;
            }

            await ctx.Response.StartAsync();
            
            if (errorFeature is not null && !ctx.Response.HasStarted)
            {
                ctx.Response.Clear();
                ctx.Response.StatusCode = 500;
            }
        });
        return proxyBuilder;
    }
}