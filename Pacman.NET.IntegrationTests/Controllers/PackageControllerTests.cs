using System.Net;
using Pacman.NET.IntegrationTests.Utilities;

namespace Pacman.NET.IntegrationTests;

public class PackageControllerTests : AbstractTestFixture
{
    [Test]
    public async Task UploadPackage_ValidPackage_ShouldSucceed()
    {
        var formData = new MultipartFormDataContent();
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestContent", "msquic_x86_64.pkg.tar.zst");
        var fileStream = new FileStream(filePath, FileMode.Open);
        formData.Add(new StreamContent(fileStream), "packageFile", "msquic.pkg.tar.zst");
        
        var response = await Client.PostAsync("pacman/package", formData, CancellationToken.None);
        var body = await response.Content.ReadAsStringAsync();
        
        Assert.That(response.IsSuccessStatusCode, Is.True, body);
    }
    
    [Test]
    public async Task UploadPackage_NoPackage_ShouldReturn400BadRequest()
    {
        var formData = new MultipartFormDataContent();
        
        var response = await Client.PostAsync("pacman/package", formData, CancellationToken.None);
        var body = await response.Content.ReadAsStringAsync();
        
        Assert.That(response.IsSuccessStatusCode, Is.False, body);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
    }
}