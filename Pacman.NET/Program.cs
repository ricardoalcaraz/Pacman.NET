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
app.UsePacmanCache();
app.MapReverseProxy(proxy =>
{
    proxy.UseLoadBalancing();
    proxy.UsePassiveHealthChecks();
    proxy.UsePackageCache();
});

app.MapControllers();

app.Run();