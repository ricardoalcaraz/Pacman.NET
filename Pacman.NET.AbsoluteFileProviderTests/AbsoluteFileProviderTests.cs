using Microsoft.Extensions.FileProviders;

namespace Pacman.NET.AbsoluteFileProviderTests;

public class AbsoluteFileProviderTests
{
    public string FilePath { get; set; }
    private IFileProvider _fileProvider;
    
    [OneTimeSetUp]
    public void Setup()
    {
        File.Delete("test.txt");
        File.Delete("test2.txt");
        File.WriteAllText("test.txt", "This is a test" + Guid.NewGuid());
        File.CreateSymbolicLink("test2.txt", "test.txt");
    }

    [SetUp]
    public void Init()
    {
        _fileProvider = new AbsoluteProvider(Directory.GetCurrentDirectory());
    }
    [Test]
    public void CheckLength()
    {
        var fileProvider = new AbsoluteProvider(Directory.GetCurrentDirectory());
        var len = fileProvider.GetFileInfo("test.txt").Length;
        var len2 = fileProvider.GetFileInfo("test2.txt").Length;
        Assert.AreEqual(len, len2);
    }
    [Test]
    public async Task CheckContents()
    {
        var len = await GetTextFromFile("test.txt");
        var len2 = await GetTextFromFile("test2.txt");
        Assert.That(len2, Is.EqualTo(len));
    }

    private async Task<string> GetTextFromFile(string fileName)
    {
        var len = _fileProvider.GetFileInfo(fileName).CreateReadStream();
        var streamReader = new StreamReader(len);
        var text = await streamReader.ReadToEndAsync();
        return text;
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        File.Delete("test.txt");
        File.Delete("test2.txt");
    }
}