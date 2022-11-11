using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Pacman.NET.Options;

namespace Pacman.NET.Services;

public interface IPacmanService
{
    IAsyncEnumerable<string> AddToRepositoryWithOutputStream(FileInfo fileInfo, CancellationToken ctx = default);
    Task<string> AddToRepository(FileInfo fileInfo, CancellationToken ctx = default);
    Task<Stream> GetPackageStream(string path, CancellationToken ctx);
}

public class PacmanService : IPacmanService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PacmanService> _logger;
    private readonly PacmanOptions _pacmanOptions;

    private readonly ConcurrentDictionary<string, FileInfo> packageLock = new();
    private volatile bool _isDownloading;


    public PacmanService(IOptions<PacmanOptions> options, ILogger<PacmanService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _pacmanOptions = options.Value;
    }


    public async IAsyncEnumerable<string> AddToRepositoryWithOutputStream(FileInfo fileInfo, [EnumeratorCancellation] CancellationToken ctx = default)
    {
        using var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/repo-add",
                //Arguments = $"{_pacmanOptions.RepoName} {fileInfo.FullName}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        proc.Start();
        string? output;
        while (!string.IsNullOrEmpty(output = await proc.StandardOutput.ReadLineAsync(ctx)))
        {
            yield return output;
        }
    }


    public async Task<string> AddToRepository(FileInfo fileInfo, CancellationToken ctx = default)
    {
        using var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/repo-add",
                //Arguments = $"{_pacmanOptions.RepoName} {fileInfo.FullName}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        proc.Start();

        var output = await proc.StandardOutput.ReadToEndAsync(ctx);

        return output;
    }


    public async Task<Stream> GetPackageStream(string path, CancellationToken ctx)
    {
        var requestUri = GetMirror(path);
        using var httpClient = _httpClientFactory.CreateClient();

        //do a check to see if the server even contains the file we're interested in
        var contentInfo = await httpClient.SendAsync(new HttpRequestMessage
        {
            Method = HttpMethod.Head,
            RequestUri = requestUri,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
        }, ctx);
        contentInfo.EnsureSuccessStatusCode();

        return await httpClient.GetStreamAsync(requestUri, ctx);
    }


    public Uri GetMirror(string path)
    {
        //pick a mirror using the round robin approach
        return new Uri($"https://mirrors.bloomu.edu{path}");
    }
}

public record PacmanPackageFile : IFileInfo
{
    private readonly FileInfo _fileInfo;


    public PacmanPackageFile(string path)
    {
        _fileInfo = new FileInfo(path);
    }


    public Stream CreateReadStream()
    {
        throw new NotImplementedException();
    }


    public bool Exists => _fileInfo.Exists;
    public long Length => _fileInfo.Length;
    public string? PhysicalPath => _fileInfo.FullName;
    public string Name => _fileInfo.Name;
    public DateTimeOffset LastModified => _fileInfo.LastWriteTime;
    public bool IsDirectory => false;
}