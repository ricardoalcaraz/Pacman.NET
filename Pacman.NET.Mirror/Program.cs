using System.Text.Encodings.Web;
using Microsoft.AspNetCore.StaticFiles;
using Pacman.NET.AbsoluteFileProvider;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Logging.AddConsole();
builder.Host.UseSystemd();

var app = builder.Build();

var path = app.Configuration["MirrorDir"];
app.UseFileServer(new FileServerOptions
{
    RequestPath = "/archlinux",
    FileProvider = Directory.Exists(path) || app.Environment.IsDevelopment()
        ? app.Environment.ContentRootFileProvider
        : new AbsoluteProvider(path!),
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

app.MapGet("/ping", () => Results.Ok("pong"));

app.Run();