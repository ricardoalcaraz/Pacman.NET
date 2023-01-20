using System.Threading.Channels;

namespace Pacman.NET.Services;

public class PersistentFileService : BackgroundService
{
    const UnixFileMode GLOBAL_READ = UnixFileMode.UserRead | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
    const UnixFileMode FILE_PERM = GLOBAL_READ | UnixFileMode.UserWrite;
    
    private readonly ILogger<PersistentFileService> _logger;
    private readonly IOptions<PacmanOptions> _pacmanOptions;
    private readonly Channel<PackageCacheRequest> _channel;

    public PersistentFileService(ILogger<PersistentFileService> logger, IOptions<PacmanOptions> pacmanOptions)
    {
        _logger = logger;
        _pacmanOptions = pacmanOptions;
        _channel = Channel.CreateUnbounded<PackageCacheRequest>();
    }
    
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        //use retry policy
        try
        {
            await foreach (var persistPackageRequest in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                _logger.LogInformation("Processing {FileName}", persistPackageRequest);

                await using (persistPackageRequest.PackageStream)
                {
                    if (persistPackageRequest.PackageStream.CanSeek)
                    {
                        persistPackageRequest.PackageStream.Seek(0, SeekOrigin.Begin);
                    }
                    
                    await using (persistPackageRequest.SaveStream)
                    {
                        
                    }
                    
                    
                    var filePath = Path.Combine(_pacmanOptions.Value.CacheDirectory, persistPackageRequest.PackageName);
                    try
                    {
                        await using var fileStream = new FileStream(filePath, new FileStreamOptions
                        {
                            Access = FileAccess.Write,
                            Mode = FileMode.Create,
                            Options = FileOptions.WriteThrough,
                            Share = FileShare.None,
                            UnixCreateMode = FILE_PERM
                        });
                        await persistPackageRequest.PackageStream.CopyToAsync(fileStream, stoppingToken);
                        await fileStream.FlushAsync(stoppingToken);
                    }
                    //persist file
                    catch (Exception)
                    {
                        var fileInfo = new FileInfo(filePath);
                        if (fileInfo.Exists)
                        {
                            _logger.LogInformation("File {FileName} still exists deleting",
                                persistPackageRequest.PackageName);
                            fileInfo.Delete();
                        }
                    }
                    finally
                    {
                        await persistPackageRequest.PackageStream.DisposeAsync();
                    }
                    _logger.LogInformation("Saved {FileName} to {FilePath}", persistPackageRequest.PackageName, filePath);

                }
                
                
                
            }

        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Shutting down background save service...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in background save service");
            throw;
        }
        finally
        {
            _channel.Writer.TryComplete();
        }
        
        _logger.LogInformation("Shut down background service safely");
    }

    public ValueTask EnqueueRequest(PackageCacheRequest request)
    {
        _logger.LogInformation("Adding {Request} to queue", request);
        
        var savePath = new FileStream(Path.Combine("", request.PackageName), new FileStreamOptions
        {
            Access = FileAccess.Write,
            Mode = FileMode.Create,
            Options = FileOptions.WriteThrough,
            Share = FileShare.None,
            UnixCreateMode = FILE_PERM
        });
        
        return _channel.Writer.WriteAsync(request);
    }
}

public record PackageCacheRequest
{
    public string PackageName { get; init; }
    public Stream PackageStream { get; init; }
    public FileStream SaveStream { get; init; }
}

