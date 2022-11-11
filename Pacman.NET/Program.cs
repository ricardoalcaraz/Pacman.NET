using System.Security.Authentication;
using Pacman.NET.Utilities;

var builder = WebApplication.CreateBuilder(args);
builder.AddPacmanServer();
//builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var clusters = new[]
{
    new ClusterConfig()
    {
        ClusterId = "cluster1",
        Destinations = new Dictionary<string, DestinationConfig>()
        {
            { "destination1", new DestinationConfig() { Address = "https://localhost:10000" } }
        },
        HttpClient = new HttpClientConfig { MaxConnectionsPerServer = 10, SslProtocols = SslProtocols.Tls12 }
    }
};
builder.Services.AddReverseProxy().LoadFromMemory(new RouteConfig[]{}, clusters);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.UsePacmanCache();

app.MapControllers();



app.Run();