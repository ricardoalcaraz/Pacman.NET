using Microsoft.Extensions.FileProviders;
using Pacman.NET.Utilities;

var builder = WebApplication.CreateBuilder(args);

builder.AddPacmanServer();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSystemd();

builder.Services
    .AddSignalR()
    .AddMessagePackProtocol();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseRouting();
    
app.UseAuthorization();

var mirrorPath = app.Configuration["MirrorPath"];
if (mirrorPath is not null && Directory.Exists(mirrorPath))
{
    app.Logger.LogInformation("Serving files from {MirrorPath}", mirrorPath);
    var fileServerOptions = new FileServerOptions
    {
        RequestPath = "/archlinux",
        FileProvider = new PhysicalFileProvider(mirrorPath),
        RedirectToAppendTrailingSlash = false,
        EnableDirectoryBrowsing = true,
        EnableDefaultFiles = false,
        StaticFileOptions =
        {
            DefaultContentType = "application/octet-stream",
            ServeUnknownFileTypes = true
        }
    };

    app.UseFileServer(fileServerOptions);
}
app.UsePacmanCache();
app.MapReverseProxy(proxy =>
{
    proxy.UseLoadBalancing();
    proxy.UsePassiveHealthChecks();
    proxy.UsePackageCache();
});

app.MapControllers();

app.Run();