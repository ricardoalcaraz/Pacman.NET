using Microsoft.AspNetCore.Http.Features;
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
        builder.Services.AddHostedService<CustomRepoService>();
        builder.Services.AddHttpClient<MirrorClient>();
        builder.Services.AddSingleton<PacmanConfigParser>();
        
        var mirrorConfigSetting = builder.Configuration["Pacman:MirrorUrl"] ?? throw new InvalidOperationException("MirrorUrl is a required config setting");
        var mirrorUri = new Uri(mirrorConfigSetting, UriKind.Absolute);
        
        if (File.Exists(mirrorUri.AbsoluteUri))
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
                    opt.CacheDirectory = Path.Combine(homeDirectory, ".cache", "pacnet");
                }
            })
            .PostConfigure(opt =>
            {
                Directory.CreateDirectory(opt.CacheDirectory);
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
    
    public static IReverseProxyApplicationBuilder UsePacmanCache(this IReverseProxyApplicationBuilder app)
    {
        var pacmanOptions = app.ApplicationServices.GetRequiredService<IOptions<PacmanOptions>>().Value;

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
        proxyBuilder.UseMiddleware<PackageCacheMiddleware>();
        return proxyBuilder;
    }
}