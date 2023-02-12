using Microsoft.Extensions.FileProviders;

namespace Pacman.NET.AbsoluteFileProviderTests;

public class AbsoluteFileProviderTests
{
    public string FilePath { get; set; }
    private readonly IFileProvider _fileProvider = new AbsoluteProvider(Directory.GetCurrentDirectory());
    
    [OneTimeSetUp]
    public void Setup()
    {
        File.Delete("test.txt");
        File.Delete("test2.txt");
        File.WriteAllText("test.txt", "This is a test" + Guid.NewGuid());
        File.CreateSymbolicLink("test2.txt", "test.txt");
    }

    [Test]
    public void Test1()
    {
        var fileProvider = new AbsoluteProvider(Directory.GetCurrentDirectory());
        var len = fileProvider.GetFileInfo("test.txt").Length;
        var len2 = fileProvider.GetFileInfo("test2.txt").Length;
        Assert.AreEqual(len, len2);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        File.Delete("test.txt");
        File.Delete("test2.txt");
    }
}