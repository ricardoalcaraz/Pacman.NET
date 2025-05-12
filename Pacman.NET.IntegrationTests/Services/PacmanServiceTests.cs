using static NUnit.Framework.Assert;

namespace Pacman.NET.IntegrationTests.Services;

public class PacmanServiceTests : WebAppFixture, IDisposable
{
    private IPacmanService _pacmanService;

    [SetUp]
    public void Init()
    {
        _pacmanService = _webAppFactory.Services.GetRequiredService<IPacmanService>();
    }
    
    [Test]
    public async Task TestDependency_PacmanExists_ShouldSucceed()
    {
        bool isValid = await _pacmanService.TestDependencies(CancellationToken.None);
        Console.WriteLine(isValid);
        That(isValid, Is.True);
    }

    [TearDown]
    public void Dispose()
    {
        _pacmanService.Dispose();
    }
}