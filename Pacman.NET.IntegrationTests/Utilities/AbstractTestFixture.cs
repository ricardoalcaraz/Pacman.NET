namespace Pacman.NET.IntegrationTests.Utilities;

public class WebAppFixture
{
    public WebApplicationFactory<PacmanOptions> WebAppFactory = null!;
    protected HttpClient Client => WebAppFactory.CreateDefaultClient();

    [OneTimeSetUp]
    public void Setup()
    {
        WebAppFactory = new CustomWebAppFactory();
    }


    [OneTimeTearDown]
    public async Task Cleanup()
    {
        await WebAppFactory.DisposeAsync();
    }
}

public class CustomWebAppFactory : WebApplicationFactory<PacmanOptions>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(s =>
        {
            var currentDir = Directory.GetCurrentDirectory();
            var options = new OptionsWrapper<PacmanOptions>(new PacmanOptions
            {
                BaseAddress = "/archlinux",
                CacheDirectory = $"{currentDir}/Content",
                MirrorUrl = ""
            });
            s.AddSingleton<IOptions<PacmanOptions>>(options);
        });
    }
}