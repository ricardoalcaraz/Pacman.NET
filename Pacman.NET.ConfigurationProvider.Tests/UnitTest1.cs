using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Pacman.NET.ConfigurationProvider.Tests;

public class ConfigurationProviderTests
{
    private IConfiguration _config;
    [SetUp]
    public void Setup()
    {
        var host = Host.CreateApplicationBuilder();
        host.Configuration.AddLinuxConfigFile("./pacman.conf", optional: false);
        
        var app = host.Build();
        _config = app.Services.GetRequiredService<IConfiguration>();
    }

    [Test]
    public void Test1()
    {
        foreach (var conf in _config.GetChildren())
        {
            Console.WriteLine($"{conf.Key}:{conf.Value}");
            Console.WriteLine(conf);
        }
        Assert.Pass();
    }
}