using Microsoft.Extensions.FileProviders;
using Pacman.NET.Middleware;

namespace Pacman.NET.Utilities;

public static class WebApplicationExtensions
{
    public static WebApplicationBuilder AddPacmanServer(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<PackageCacheMiddleware>();
        builder.Services.AddSingleton<MirrorSyncService>();
        builder.Services.AddSingleton<PacmanService>();
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
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        builder.Services.AddMemoryCache();
        builder.Services.AddOptions<ApplicationOptions>()
            .BindConfiguration("PacmanConfig")
            .Configure(opt =>
            {
                var cacheUri = new Uri(opt.CustomRepoDir, UriKind.RelativeOrAbsolute);
                
                if (!cacheUri.IsAbsoluteUri)
                {
                    opt.CustomRepoDir = Path.Combine(builder.Environment.ContentRootPath, opt.CustomRepoDir);
                }

                Directory.CreateDirectory(opt.CustomRepoDir);
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        builder.Services.AddHttpClient();
        return builder;
    }
    
    public static WebApplication UsePacmanCache(this WebApplication app)
    {
        var pacmanOptions = app.Services.GetRequiredService<IOptions<PacmanOptions>>().Value;

        var compositeProvider = new CompositeFileProvider(
            new PhysicalFileProvider(pacmanOptions.CacheDirectory),
            new PhysicalFileProvider(pacmanOptions.DbDirectory),
            new PhysicalFileProvider(pacmanOptions.SaveDirectory)
        );
        
        app.UseFileServer(new FileServerOptions
        {
            RequestPath = pacmanOptions.BaseAddress,
            FileProvider = compositeProvider,
            EnableDefaultFiles = false,
            RedirectToAppendTrailingSlash = true,
            EnableDirectoryBrowsing = true,
            StaticFileOptions =
            {
                DefaultContentType = "application/octet-stream",
                ServeUnknownFileTypes = true
            }
        });
        
        app.UseMiddleware<PackageCacheMiddleware>();

        return app;
    }
}