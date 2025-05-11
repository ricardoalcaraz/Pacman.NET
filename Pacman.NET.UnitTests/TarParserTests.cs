using Microsoft.Extensions.Logging.Abstractions;
using Pacman.Extensions.FileProviders.AppleArchiveProvider;
using Pacman.NET.Utilities;

namespace Pacman.NET.UnitTests;

[TestClass]
public class ArchiveReaderTests
{
    [TestMethod]
    public async Task ReadHeader_ContainsValidInfo_ShouldSucceed()
    {
        await using var tarFile = File.Open($"./Content/core.db", FileMode.Open);
        var ArchiveReader = new ArchiveReader(new NullLogger<ArchiveReader>());
        await ArchiveReader.ParseTarFile(tarFile);
        //Assert.Fail();
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