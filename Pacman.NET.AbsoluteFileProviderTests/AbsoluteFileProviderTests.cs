using Microsoft.Extensions.FileProviders;

namespace Pacman.NET.AbsoluteFileProviderTests;

public class AbsoluteFileProviderTests
{
    private IFileProvider _fileProvider = null!;
    private const string PHYSICAL_FILE_NAME = "test.txt";
    private const string SYMBOLIC_FILE_NAME = "symbolic.txt";
    private string _absolutePath = null!;
    private string _tempFolder = null!;
    private FileInfo _physicalFileInfo = null!;
    
    
    [OneTimeSetUp]
    public void Setup()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), "tests");
        Directory.CreateDirectory(_tempFolder);
        _absolutePath = Path.Combine(_tempFolder, PHYSICAL_FILE_NAME);
        File.Delete(_absolutePath);
        var symbolicPath = Path.Combine(_tempFolder, SYMBOLIC_FILE_NAME); 
        File.Delete(symbolicPath);
        File.WriteAllText(_absolutePath, "This is a test" + Guid.NewGuid());
        File.CreateSymbolicLink(symbolicPath, _absolutePath);
    }

    
    [SetUp]
    public void Init()
    {
        _physicalFileInfo = new FileInfo(_absolutePath);
        _fileProvider = new AbsoluteProvider(Path.Combine(Path.GetTempPath(), "tests"));
    }
    
    
    [Test]
    public void CheckLength()
    {
        var symbolicFile = _fileProvider.GetFileInfo(SYMBOLIC_FILE_NAME);
        
        Assert.That(_physicalFileInfo.Length, Is.EqualTo(symbolicFile.Length));
    }
    
    
    [Test]
    public void CheckName()
    {
        var fileInfo = _fileProvider.GetFileInfo(SYMBOLIC_FILE_NAME);
        
        Assert.That(fileInfo.Name, Is.EqualTo(SYMBOLIC_FILE_NAME));
    }
    
    [Test]
    public async Task CheckContents()
    {
        var physicalFileContents = await new StreamReader(_physicalFileInfo.OpenRead()).ReadToEndAsync();
        var symbolicFileContent = await new StreamReader(_fileProvider.GetFileInfo(SYMBOLIC_FILE_NAME).CreateReadStream()).ReadToEndAsync();
        Assert.That(physicalFileContents, Is.EqualTo(symbolicFileContent));
    }

    
    [Test]
    public void CheckFilePath()
    {
        var fileInfo = _fileProvider.GetFileInfo(SYMBOLIC_FILE_NAME);
        
        Assert.That(fileInfo.PhysicalPath, Is.EqualTo(_physicalFileInfo.FullName));
        Assert.That(fileInfo.IsDirectory, Is.False);
    }

    
    [Test]
    public void CheckDirectory()
    {
        var fileInfo = _fileProvider.GetFileInfo(SYMBOLIC_FILE_NAME);
        
        Assert.That(fileInfo.IsDirectory, Is.False);
    }
    
    
    [Test]
    public void CheckExists()
    {
        var fileInfo = _fileProvider.GetFileInfo(SYMBOLIC_FILE_NAME);
        
        Assert.That(fileInfo.Exists, Is.True);
    }

    
    [Test]
    public void CheckDirectoryContents()
    {
        var fileProvider = new AbsoluteProvider(_tempFolder);
        var contents = fileProvider.GetDirectoryContents("").ToList();
        Assert.That(contents.Count, Is.EqualTo(2));
    }
    
    
    [OneTimeTearDown]
    public void TearDown()
    {
        File.Delete(PHYSICAL_FILE_NAME);
        File.Delete("test2.txt");
    }
}