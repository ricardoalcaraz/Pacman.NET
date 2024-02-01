using System.Net;
using Microsoft.AspNetCore.Http;

namespace Pacman.NET.IntegrationTests.Middleware;

public class PackageCacheMiddlewareTests : WebAppFixture
{
    [SetUp]
    public Task Initialize() => Task.CompletedTask;
    
    [Test]
    public async Task GetPackage_PackageDoesNotExist_ShouldReturn404()
    {
        var response = await Client.GetAsync("/archlinux/core/os/x86_64/bzip2-1.0.8-4-x86_64.pkg.tar.zst");
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        Assert.Multiple(() =>
        {
            Assert.That(stream, Is.Not.Null);
            Assert.That(memoryStream.Length, Is.EqualTo(0));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }
    
    [Test]
    public async Task GetPackage_PackageDoesNotHaveName_ShouldReturn404()
    {
        var response = await Client.GetAsync("/archlinux/core/os/");
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        Assert.Multiple(() =>
        {
            Assert.That(stream, Is.Not.Null);
            Assert.That(memoryStream.Length, Is.EqualTo(0));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }
    
    [Test]
    public async Task GetPackage_PackageExists_ShouldReturnOk()
    {
        var fileLength = 0;
        var response = await Client.GetAsync("/archlinux/core/os/x86_64/bzip2-1.0.8-4-x86_64.pkg.tar.zst");
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        Assert.Multiple(() =>
        {
            Assert.That(stream, Is.Not.Null);
            Assert.That(memoryStream.Length, Is.EqualTo(fileLength));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    [Test]
    public async Task GetPackage_ModifiedSince_ShouldReturn304()
    {
        var httpRequestMsg = new HttpRequestMessage(HttpMethod.Get, "/archlinux/core/os/x86_64/bzip2-1.0.8-4-x86_64.pkg.tar.zst");
        httpRequestMsg.Headers.IfModifiedSince = DateTimeOffset.Now;
        var response = await Client.SendAsync(httpRequestMsg);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotModified));
        var content = await response.Content.ReadAsByteArrayAsync();
        Assert.That(content, Is.Empty);
    }
    
}