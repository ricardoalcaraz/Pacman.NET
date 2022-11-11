using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<IPacmanService, PacmanService>();
builder.Services.AddOptions<PacmanOptions>()
    .BindConfiguration("Pacman")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHttpClient();


builder.Services.AddTransient<IPacmanService, PacmanService>();
builder.Services.AddHostedService<MirrorSyncService>();


var fileProvider = builder.Environment.IsDevelopment()
    ? new PhysicalFileProvider(builder.Environment.ContentRootPath)
    : new PhysicalFileProvider("/data/pacman");

builder.Services.AddOptions<PackageCacheOptions>()
    .Configure(opt =>
    {
        opt.SavePath = "/data/pacman";
        opt.FileProvider = fileProvider;
    });
builder.Services.AddOptions<FileServerOptions>()
    .Configure<IOptions<PackageCacheOptions>>((opt, cacheOptions) =>
    {
        opt.RequestPath = "/archlinux";
        opt.FileProvider = cacheOptions.Value.FileProvider;
        opt.EnableDefaultFiles = false;
        opt.RedirectToAppendTrailingSlash = false;
        opt.EnableDirectoryBrowsing = true;
        opt.StaticFileOptions.ServeUnknownFileTypes = true;
        opt.DirectoryBrowserOptions.RequestPath = "/archlinux";
        opt.StaticFileOptions.DefaultContentType = "application/octet-stream";
    });



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

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


app.MapControllers();

app.Run();