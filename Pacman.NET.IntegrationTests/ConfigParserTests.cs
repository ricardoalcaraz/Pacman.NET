namespace Pacman.NET.IntegrationTests;

public class 
    ConfigParserTests : WebAppFixture
{
    private string _mirrorlist = File.ReadAllText("./Config/Mirrorlist");

    [Test]
    public async Task ParseList_ValidList_ShouldSucceed()
    {
        var configParser = new PacmanConfigParser(null);
        await foreach (var mirror in configParser.ParseMirrorlist("./Config/Mirrorlist", CancellationToken.None))
        {
            if (string.IsNullOrWhiteSpace(mirror))
            {
                Assert.Fail();
            }
        }
    }
}