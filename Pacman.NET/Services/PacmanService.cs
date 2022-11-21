using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.FileProviders;

namespace Pacman.NET.Services;

public interface IPacmanService : IDisposable
{
    AddRepoResponse AddPackage(Stream packageStream, CancellationToken ctx = default);
    Task<Stream> GetPackageStream(string path, CancellationToken ctx);
    Task<string> TestDependencies(CancellationToken ctx);
}

public class PacmanService : IPacmanService
{
    private const string REPO_ADD_BIN = "/usr/bin/repo-add";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PacmanService> _logger;
    private readonly PacmanOptions _pacmanOptions;
    private readonly Process _repoAddProcess = new();
    private readonly ConcurrentDictionary<string, FileInfo> packageLock = new();
    private volatile bool _isDownloading;


    public PacmanService(IOptions<PacmanOptions> options, ILogger<PacmanService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _pacmanOptions = options.Value;
    }


    public async Task<string> TestDependencies(CancellationToken ctx = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "/usr/bin/repo-elephant"
        };
        _repoAddProcess.StartInfo = startInfo;
        try
        {
            if (_repoAddProcess.Start())
            {
                await _repoAddProcess.WaitForExitAsync(ctx);
                var elephant = await _repoAddProcess.StandardOutput.ReadToEndAsync(ctx);
                return elephant;
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to verify existence of pacman");
        }
        
        return string.Empty;
    }
    
    
    public IAsyncEnumerable<string> AddToRepositoryWithOutputStream(FileInfo fileInfo, CancellationToken ctx = default)
    {
        if (!File.Exists(REPO_ADD_BIN))
        {
            throw new InvalidOperationException("The repo-add executable does not exist!");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = REPO_ADD_BIN,
            //Arguments = $"{_pacmanOptions.RepoName} {fileInfo.FullName}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        _repoAddProcess.StartInfo = startInfo;
        
        return ReadOutputStream(_repoAddProcess.StandardOutput, ctx);
    }


    public AddRepoResponse AddPackage(Stream packageStream, CancellationToken ctx = default)
    {
        using var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = REPO_ADD_BIN,
                Arguments = "--verify -n --sign --prevent-downgrade",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        proc.Start();
        var exitCode = proc.ExitCode;
        if (exitCode > 0)
        {
            return new AddRepoResponse
            {
                ExitCode = 0,
                Output = proc.StandardError,
                Signature = string.Empty
            };
        }

        return new AddRepoResponse
        {
            ExitCode = 0,
            Output = proc.StandardOutput,
            Signature = string.Empty
        };
    }

    private async IAsyncEnumerable<string> ReadOutputStream(StreamReader outputStream, CancellationToken ctx)
    {
        while (!outputStream.EndOfStream)
        {
            ctx.ThrowIfCancellationRequested();
            var outputLine = await outputStream.ReadLineAsync(ctx);
            _logger.LogDebug("{Output}", outputLine);
            yield return outputLine ?? string.Empty;
        }
        await _repoAddProcess.WaitForExitAsync(ctx);
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

    public void Dispose()
    {
        _repoAddProcess.Dispose();
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

public record AddRepoResponse
{
    public required int ExitCode { get; init; }
    public required StreamReader Output { get; init; }
    public required string Signature { get; init; }
}