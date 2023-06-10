using System.Formats.Tar;
using Microsoft.Extensions.FileProviders;
using Pacman.NET.Middleware;
using Pacman.NET.Utilities;

var builder = WebApplication.CreateBuilder(args);
if (OperatingSystem.IsLinux())
{
    builder.Configuration.AddIniFile("/etc/pacman.conf", true, true);
}

void WriteFolderPath(Environment.SpecialFolder folder)
{
    Console.WriteLine($"{folder.ToString()}:{Environment.GetFolderPath(folder)}");
}

void WriteAllFolders()
{
    var enumValues = Enum.GetValues<Environment.SpecialFolder>();
    List<Environment.SpecialFolder> emptyFolders = new();
    foreach (var specialFolder in enumValues)
    {
        var path = Environment.GetFolderPath(specialFolder, Environment.SpecialFolderOption.None);
        if (string.IsNullOrWhiteSpace(path))
        {
            emptyFolders.Add(specialFolder);
        }
        else
        {
            WriteFolderPath(specialFolder);
        }
    }

    foreach (var em in emptyFolders)
    {
        Console.WriteLine($"No path: {em}");
    }
}
WriteAllFolders();

builder.AddPacmanServer();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSystemd();

builder.Services
    .AddSignalR()
    .AddMessagePackProtocol();
var cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "Pacman.NET", "cache");

builder.Services.AddOptions<RepositoryOptions>()
    .BindConfiguration("Pacman")
    .Configure(opt =>
    {
        opt.RepoDirectory = builder.Environment.IsDevelopment()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".local", "Pacman.NET", "repos")
            : builder.Configuration["Pacman:RepositoryDirectory"] ?? throw new InvalidOperationException();

        opt.PackageDirectory = builder.Environment.IsDevelopment() 
            ? cachePath
            : builder.Configuration["Pacman:CacheDirectory"] ?? throw new InvalidOperationException();
        opt.RepoDirectory = Directory.CreateDirectory(opt.RepoDirectory).FullName;
        opt.PackageDirectory = Directory.CreateDirectory(opt.PackageDirectory).FullName;

        opt.RepositoryProvider = new PhysicalFileProvider(opt.RepoDirectory);
        opt.PackageProvider = new PhysicalFileProvider(opt.PackageDirectory);
    });

var app = builder.Build();

var cacheDir = new DirectoryInfo(app.Configuration["Options:CacheDir"] ?? cachePath);
if (cacheDir.Exists)
{
    app.Logger.LogInformation("CacheDir exists at {Path}", cacheDir);
}
else
{
    app.Logger.LogWarning("CacheDir not specified");
}
var options = app.Services.GetRequiredService<IOptions<RepositoryOptions>>().Value;
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseMiddleware<PackageCacheMiddleware>();
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = options.BaseAddress,
    FileProvider = options.RepositoryProvider,
    RedirectToAppendTrailingSlash = false,
    DefaultContentType = "application/octet-stream",
    ServeUnknownFileTypes = true,
});

app.MapReverseProxy(proxy =>
{
    proxy.UseLoadBalancing();
    proxy.UsePassiveHealthChecks();
    proxy.UsePacmanCache();
});
Console.Error.WriteLine("Starting program...");

app.Run();
Console.Error.WriteLine("Terminating program...");