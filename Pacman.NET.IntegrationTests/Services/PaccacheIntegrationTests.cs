using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Pacman.NET.IntegrationTests.Services;

public class PaccacheIntegrationTests : WebAppFixture
{
    public PaccacheIntegrationTests()
    {
    }

    [Test]
    public async Task RunService()
    {
        var service = new PaccacheBackgroundService(new NullLogger<PaccacheBackgroundService>(), null);
        await service.StartAsync(CancellationToken.None);
    }
}