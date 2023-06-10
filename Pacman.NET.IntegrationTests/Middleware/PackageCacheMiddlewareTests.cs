using System.Net;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Pacman.NET.IntegrationTests.Middleware;

public class RedisLogger : ILogger
{
    private static readonly Channel<string> LogMessages = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
    {
        AllowSynchronousContinuations = true,
        SingleReader = true,
        SingleWriter = false
    });

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

    public bool IsEnabled(LogLevel logLevel)
    {
        return LogMessages.Reader.Completion.IsCompleted;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            var info = formatter(state, exception);
            LogMessages.Writer.TryWrite(info);
        }
    }
}
public class PackageCacheMiddlewareTests : WebAppFixture
{
    [SetUp]
    public Task Initialize() => Task.CompletedTask;
    
    [Test]
    public async Task GetPackage_PackageDoesNotExist_ShouldProxyRequest()
    {
        var response = await Client.GetAsync("/archlinux/core/os/x86_64/bzip2-1.0.8-4-x86_64.pkg.tar.zst");
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        Assert.Multiple(() =>
        {
            Assert.That(stream, Is.Not.Null);
            Assert.That(memoryStream.Length, Is.EqualTo(0));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }
    
    [Test]
    public async Task GetPackage_PackageDoesNotHaveName_ShouldReturn404()
    {
        var response = await Client.GetAsync("/archlinux/core/os/");
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        Assert.Multiple(() =>
        {
            Assert.That(stream, Is.Not.Null);
            Assert.That(memoryStream.Length, Is.EqualTo(0));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }
    
    [Test]
    public async Task GetPackage_PackageExists_ShouldReturnOk()
    {
        var fileLength = 0;
        var response = await Client.GetAsync("/archlinux/core/os/x86_64/bzip2-1.0.8-4-x86_64.pkg.tar.zst");
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        Assert.Multiple(() =>
        {
            Assert.That(stream, Is.Not.Null);
            Assert.That(memoryStream.Length, Is.EqualTo(fileLength));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }
    
    public class HttpResponseProvider : HttpResponse
    {
        public override void OnStarting(Func<object, Task> callback, object state)
        {
            throw new NotImplementedException();
        }

        public override void OnCompleted(Func<object, Task> callback, object state)
        {
            throw new NotImplementedException();
        }

        public override void Redirect(string location, bool permanent)
        {
            throw new NotImplementedException();
        }

        public override HttpContext HttpContext { get; }
        public override int StatusCode { get; set; }
        public override IHeaderDictionary Headers { get; }
        public override Stream Body { get; set; }
        public override long? ContentLength { get; set; }
        public override string? ContentType { get; set; }
        public override IResponseCookies Cookies { get; }
        public override bool HasStarted { get; }
    }
}