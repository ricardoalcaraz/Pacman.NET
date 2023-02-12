using Pacman.NET.AbsoluteFileProvider;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDirectoryBrowser();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.UseFileServer(new FileServerOptions
{
    RequestPath = "/archlinux/ricardoalcaraz.dev",
    FileProvider = new AbsoluteProvider("/data/archlinux/mirror"),
    RedirectToAppendTrailingSlash = true,
    EnableDirectoryBrowsing = true,
    EnableDefaultFiles = false,
    StaticFileOptions =
    {
        DefaultContentType = "application/octet-stream", 
        ServeUnknownFileTypes = true
    }
});
app.Run();