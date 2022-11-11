using System.Diagnostics;
using Microsoft.Extensions.FileProviders;

namespace Pacman.NET.Utilities;

public static class WebApplicationExtensions
{
    public static WebApplicationBuilder AddPacmanServer(this WebApplicationBuilder builder)
    {

        builder.Services.AddOptions<PacmanOptions>()
            .BindConfiguration("Pacman")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<PacmanOptions>()
            .BindConfiguration("ApplicationOptions")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder.Services.AddTransient<IPacmanService, PacmanService>();
        builder.Services.AddHostedService<MirrorSyncService>();
        builder.Services.AddHttpClient();

        return builder;
    }
    
    public static WebApplication UsePacmanCache(this WebApplication app)
    {
        var fileProvider = app.Environment.IsDevelopment()
            ? new PhysicalFileProvider(app.Environment.ContentRootPath)
            : new PhysicalFileProvider("/data/pacman");

        
        app.UseFileServer(new FileServerOptions
        {
            RequestPath = "/archlinux",
            FileProvider = fileProvider,
            EnableDefaultFiles = false,
            RedirectToAppendTrailingSlash = false,
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