using static NUnit.Framework.Assert;

namespace Pacman.NET.IntegrationTests.Services;


public class PacmanServiceTests : WebAppFixture
{
    private IPacmanService _pacmanService = null!;

    [SetUp]
    public void Init()
    {
        _pacmanService = WebAppFactory.Services.GetRequiredService<IPacmanService>();
    }
    
    [Test]
    public async Task TestDependency_PacmanExists_ShouldSucceed()
    {
        var elephant = await _pacmanService.TestDependencies(CancellationToken.None);
        Console.WriteLine(elephant);
        That(string.IsNullOrWhiteSpace(elephant), Is.False);
    }
}