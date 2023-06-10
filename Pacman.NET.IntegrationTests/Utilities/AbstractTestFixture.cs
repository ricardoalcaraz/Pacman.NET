namespace Pacman.NET.IntegrationTests.Utilities;

public class WebAppFixture
{
    public WebApplicationFactory<PacmanOptions> _webAppFactory = null!;
    protected HttpClient Client => _webAppFactory.CreateDefaultClient();

    [OneTimeSetUp]
    public void Setup()
    {
        _webAppFactory = new CustomWebAppFactory();
    }


    [OneTimeTearDown]
    public async Task Cleanup()
    {
        await _webAppFactory.DisposeAsync();
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