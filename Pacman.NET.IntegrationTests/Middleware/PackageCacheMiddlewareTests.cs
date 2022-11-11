using Pacman.NET.IntegrationTests.Utilities;

namespace Pacman.NET.IntegrationTests.Middleware;

public class PackageCacheMiddlewareTests : AbstractTestFixture
{
    public PackageCacheMiddlewareTests()
    {
        WebAppFactory = WebAppFactory
            .WithWebHostBuilder(b => { b.Configure(c => { c.UseMiddleware<PackageCacheMiddleware>(); }); });
    }
}