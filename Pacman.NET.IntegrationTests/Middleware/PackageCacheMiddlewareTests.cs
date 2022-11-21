namespace Pacman.NET.IntegrationTests.Middleware;

public class PackageCacheMiddlewareTests : WebAppFixture
{
    [SetUp]
    public async Task Initialize()
    {
        var pacmanMirror = WebAppFactory.Services.GetRequiredService<MirrorSyncService>();
        await pacmanMirror.ExecuteTask!;
    }
    
    [Test]
    public async Task GetPackage_PackageDoesNotExist_ShouldProxyRequest()
    {
        var stream = await Client.GetStreamAsync("/archlinux/core/os/x86_64/ca-certificates-20220905-1-any.pkg.tar.zst");
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        Assert.That(stream, Is.Not.Null);
        Assert.That(memoryStream.Length, Is.GreaterThan(0));
    }
}