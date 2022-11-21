using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Pacman.NET.Services;

namespace Pacman.NET.UnitTests;

[TestClass]
public class PacmanServiceTests
{
    private readonly PacmanService _pacmanService;

    public PacmanServiceTests()
    {
        var hostBuilder = Host.CreateDefaultBuilder();
        var currentDir = Directory.GetCurrentDirectory();
        hostBuilder.ConfigureServices(s =>
        {
            var options = new OptionsWrapper<PacmanOptions>(new PacmanOptions
            {
                BaseAddress = "/archlinux",
                Configuration = "",
                CacheDirectory = $"./Content",
                DbDirectory = $"{currentDir}/Db",
            });
            s.AddSingleton<IOptions<PacmanOptions>>(options);
            s.AddHttpClient();
            s.AddTransient<PacmanService>();
        });

        var host = hostBuilder.Build();
        _pacmanService = host.Services.GetRequiredService<PacmanService>();
    }
    
    [TestMethod]
    public async Task TestDependency_PacmanDoesNotExist_ShouldFail()
    {
        var elephant = await _pacmanService.TestDependencies();
        Assert.IsTrue(string.IsNullOrWhiteSpace(elephant));
    }
    
    [TestMethod]
    public async Task TestDependency_PacmanExists_ShouldSucceed()
    {
        var elephant = await _pacmanService.TestDependencies();
        Console.WriteLine(elephant);
        Assert.IsFalse(string.IsNullOrWhiteSpace(elephant));
    }

    [TestMethod]
    public async Task AddPackage_ValidPackage_ShouldSucceed()
    {
        
    }
}