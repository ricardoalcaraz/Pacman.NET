using System.Text.Encodings.Web;
using Microsoft.AspNetCore.StaticFiles;
using Pacman.NET.AbsoluteFileProvider;
using Pacman.NET.Mirror;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Logging.AddConsole();

builder.Host.UseSystemd();
builder.Services.AddHostedService<SyncMirrorService>();
builder.Services.AddOptions<SyncMirrorOptions>();
var app = builder.Build();

var path = app.Configuration["MirrorDir"];
if (Directory.Exists(path))
{
    app.UseFileServer(new FileServerOptions
    {
        RequestPath = "/archlinux",
        FileProvider = new AbsoluteProvider(path),
        RedirectToAppendTrailingSlash = true,
        EnableDirectoryBrowsing = true,
        EnableDefaultFiles = false,
        DirectoryBrowserOptions = { Formatter = new HtmlDirectoryFormatter(HtmlEncoder.Default) },
        StaticFileOptions =
        {
            ServeUnknownFileTypes = true,
            DefaultContentType = "application/octet-stream"
        }
    });
}


app.MapGet("/ping", () => Results.Ok("pong"));

app.Run();