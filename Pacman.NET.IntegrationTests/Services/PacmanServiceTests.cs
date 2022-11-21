using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pacman.NET.Services;

namespace Pacman.NET.IntegrationTests.Services;


public class PacmanServiceTests : WebAppFixture
{
    private PacmanService _pacmanService;

    [SetUp]
    public void Init()
    {
        _pacmanService = WebAppFactory.Services.GetRequiredService<PacmanService>();
    }
    
    [Test]
    public async Task TestDependency_PacmanExists_ShouldSucceed()
    {
        var elephant = await _pacmanService.TestDependencies();
        Console.WriteLine(elephant);
        Assert.IsFalse(string.IsNullOrWhiteSpace(elephant));
    }

    [Test]
    public async Task AddPackage_ValidPackage_ShouldSucceed()
    {
        
    }
}