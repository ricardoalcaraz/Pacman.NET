using Microsoft.AspNetCore.Http.Features;
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

builder.Services.AddOptions<RepositoryOptions>()
    .BindConfiguration("Pacman")
    .Configure(opt =>
    {
        opt.RepoDirectory = builder.Environment.IsDevelopment()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "Pacman.NET", "repos")
            : builder.Configuration["Pacman:RepositoryDirectory"] ?? throw new InvalidOperationException();
        
        opt.PackageDirectory = builder.Environment.IsDevelopment() 
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "Pacman.NET", "cache")
            : builder.Configuration["Pacman:CacheDirectory"] ?? throw new InvalidOperationException();
        opt.RepoDirectory = Directory.CreateDirectory(opt.RepoDirectory).FullName;
        opt.PackageDirectory = Directory.CreateDirectory(opt.PackageDirectory).FullName;

        opt.RepositoryProvider = new PhysicalFileProvider(opt.RepoDirectory);
        opt.PackageProvider = new PhysicalFileProvider(opt.PackageDirectory);


    });

var app = builder.Build();

var options = app.Services.GetRequiredService<IOptions<RepositoryOptions>>().Value;
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseRouting();

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

app.Run();