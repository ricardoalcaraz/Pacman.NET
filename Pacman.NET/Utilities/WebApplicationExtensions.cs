using Microsoft.Extensions.FileProviders;
using Pacman.NET.Middleware;

namespace Pacman.NET.Utilities;

public static class WebApplicationExtensions
{
    public static WebApplicationBuilder AddPacmanServer(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<MirrorSyncService>();
        builder.Services.AddSingleton<PacmanService>();
        builder.Services.AddSingleton<IPacmanService, PacmanService>(sp => sp.GetRequiredService<PacmanService>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<PacmanService>());
        builder.Services.AddHostedService<MirrorSyncService>(sp => sp.GetRequiredService<MirrorSyncService>());
        builder.Services.AddHttpClient<MirrorClient>();
        builder.Services.AddSingleton<PacmanConfigParser>();
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
                var cacheUri = new Uri(opt.CacheDirectory, UriKind.RelativeOrAbsolute);
                if (!cacheUri.IsAbsoluteUri)
                {
                    opt.CacheDirectory = Path.Combine(builder.Environment.ContentRootPath, opt.CacheDirectory);
                }

                Directory.CreateDirectory(opt.CacheDirectory);
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        builder.Services.AddMemoryCache();
        builder.Services.AddOptions<ApplicationOptions>()
            .BindConfiguration("Pacman")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        builder.Services.AddHttpClient();
        return builder;
    }
    
    public static WebApplication UsePacmanCache(this WebApplication app)
    {
        var pacmanOptions = app.Services.GetRequiredService<IOptions<PacmanOptions>>().Value;
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