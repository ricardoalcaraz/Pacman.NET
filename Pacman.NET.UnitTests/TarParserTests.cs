using System.Formats.Tar;
using Microsoft.Extensions.Logging.Abstractions;
using Pacman.Extensions.FileProviders.AppleArchiveProvider;
using Pacman.Extensions.FileProviders.TarProvider;
using Pacman.NET.Utilities;

namespace Pacman.NET.UnitTests;

[TestClass]
public class ArchiveReaderTests
{
    [TestMethod]
    [DataRow("Program.cs")]
    public void CodeFileChecker_IsCodeFile(string filePath)
    {
        var isCodeFile = FileChecker.IsCodeFile(filePath);
        
        Assert.IsTrue(isCodeFile);
    }
    
    
    private readonly string _tempFile = Path.GetTempFileName();
    
    [TestInitialize]
    public async Task CreateTarArchive()
    {
        var fileStream = new FileStream(_tempFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        await using TarWriter tarWriter = new TarWriter(fileStream, TarEntryFormat.Pax);
        await TarFile.CreateFromDirectoryAsync(Environment.CurrentDirectory, fileStream, true);
    }
    
    
    
    [TestMethod]
    public async Task ReadHeader_ContainsValidInfo_ShouldSucceed()
    {
        await using TarReader tarFile = new (File.Open(_tempFile, FileMode.Open));
        var archiveReader = new TarArchiveProvider(tarFile);

        foreach (var entry in archiveReader)
        {
            Assert.IsNotNull(entry);
        }
        File.Delete(_tempFile);
    }

    [TestMethod]
    public void ReadHeader_ContainsExtendedMetadata_ShouldSucceed()
    {
        //Assert.Fail();
    }
    
    [TestMethod]
    public void ReadHeader_ContainsInvalidInfo_ShouldThrowException()
    {
       // Assert.Fail();
    }

    [TestMethod]
    public void ReadFile_ContainsValidInfo_ShouldReturnFile()
    {
        //Assert.Fail();
    }
}