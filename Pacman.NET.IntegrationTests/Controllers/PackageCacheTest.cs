namespace Pacman.NET.IntegrationTests;

public class PackageCacheTest : WebAppFixture
{
    [Test]
    public async Task CustomRepo_PackageExists_ShouldProxyRequest()
    {
        var packageStream = await Client.GetStreamAsync($"archlinux/ricardoalcaraz.dev/test.tar.gz");
        using var memoryStream = new MemoryStream((int)packageStream.Length);
        await packageStream.CopyToAsync(memoryStream);
    }
    
    [Test]
    public async Task CoreRepo_PackageExists_ShouldProxyRequest()
    {
        var packageStream = await Client.GetStreamAsync($"archlinux/core/os/x86_64/test.tar.gz");
        using var memoryStream = new MemoryStream((int)packageStream.Length);
        await packageStream.CopyToAsync(memoryStream);
    }
}