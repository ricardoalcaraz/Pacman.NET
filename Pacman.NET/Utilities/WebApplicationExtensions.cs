using System.Diagnostics;
using Microsoft.Extensions.FileProviders;
using Pacman.NET.Middleware;
using Yarp.ReverseProxy.LoadBalancing;

namespace Pacman.NET.Utilities;

public static class WebApplicationExtensions
{
    public static WebApplicationBuilder AddPacmanServer(this WebApplicationBuilder builder)
    {

        builder.Services.AddReverseProxy();
        builder.Services.AddOptions<PacmanOptions>()
            .BindConfiguration("Pacman")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder.Services.AddMemoryCache();
        builder.Services.AddOptions<ApplicationOptions>()
            .BindConfiguration("ApplicationOptions")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        var inMemoryConfig = new InMemoryConfigProvider(new List<RouteConfig>(), new List<ClusterConfig>());
        builder.Services.AddSingleton<IProxyConfigProvider>(inMemoryConfig);
        builder.Services.AddSingleton(sp =>
        {
            var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var logger = sp.GetRequiredService<ILogger<MirrorSyncService>>();
            return new MirrorSyncService(clientFactory, logger, inMemoryConfig, sp.GetRequiredService<IConfiguration>());
        });

        builder.Services.AddTransient<IPacmanService, PacmanService>();
        builder.Services.AddHostedService<MirrorSyncService>(sp => sp.GetRequiredService<MirrorSyncService>());
        //builder.Services.AddHostedService<CustomRepoService>();
        builder.Services.AddHttpClient();
        builder.Services.AddTransient<PacmanService>();
        return builder;
    }
    
    public static WebApplication UsePacmanCache(this WebApplication app)
    {
        var pacmanOptions = app.Services.GetRequiredService<IOptions<PacmanOptions>>().Value;

        var cacheDir = Directory.CreateDirectory(pacmanOptions.CacheDirectory);
        app.UseMiddleware<PackageCacheMiddleware>();

        app.UseFileServer(new FileServerOptions
        {
            RequestPath = pacmanOptions.BaseAddress,
            FileProvider = new PhysicalFileProvider(pacmanOptions.CacheDirectory),
            EnableDefaultFiles = false,
            RedirectToAppendTrailingSlash = true,
            EnableDirectoryBrowsing = true,
            StaticFileOptions =
            {
                DefaultContentType = "application/octet-stream",
                ServeUnknownFileTypes = true
            }
        });

        return app;
    }
}