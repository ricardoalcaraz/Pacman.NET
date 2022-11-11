namespace Pacman.NET.IntegrationTests.Utilities;

[TestFixture]
public abstract class AbstractTestFixture
{
    protected WebApplicationFactory<PacmanOptions> WebAppFactory = null!;
    protected HttpClient Client => WebAppFactory.CreateDefaultClient();

    [OneTimeSetUp]
    public void Setup()
    {
        WebAppFactory = CreateWebAppFactory();
    }


    [OneTimeTearDown]
    public async Task Cleanup()
    {
        await WebAppFactory.DisposeAsync();
    }

    protected virtual WebApplicationFactory<PacmanOptions> CreateWebAppFactory()
    {
        return new WebApplicationFactory<PacmanOptions>();
    }
}

public class CustomWebAppFactory : WebApplicationFactory<PacmanOptions>
{
    protected override IWebHostBuilder CreateWebHostBuilder()
    {
        return base.CreateWebHostBuilder()!
            .ConfigureServices(s =>
            {
                s.AddAuthentication(opt =>
                {
                    opt.DefaultScheme = "test";
                    opt.AddScheme("test", b =>
                    {
                        
                    });
                });
            });
    }
}