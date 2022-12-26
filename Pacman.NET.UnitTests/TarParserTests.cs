using Microsoft.Extensions.Logging.Abstractions;
using Pacman.NET.Utilities;

namespace Pacman.NET.UnitTests;

[TestClass]
public class TarParserTests
{
    [TestMethod]
    public async Task ReadHeader_ContainsValidInfo_ShouldSucceed()
    {
        await using var tarFile = File.Open($"./Content/core.db", FileMode.Open);
        var tarParser = new TarParser(new NullLogger<TarParser>());
        await tarParser.ParseTarFile(tarFile);
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