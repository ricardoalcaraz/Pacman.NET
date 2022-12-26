using System.Threading.Channels;

namespace Pacman.NET.Services;

public class PersistentFileService : BackgroundService
{
    const UnixFileMode GLOBAL_READ = UnixFileMode.UserRead | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
    const UnixFileMode FILE_PERM = GLOBAL_READ | UnixFileMode.UserWrite;
    
    private readonly ILogger<PersistentFileService> _logger;
    private readonly IOptions<PersistentFileServiceOptions> _options;
    private readonly IOptions<PacmanOptions> _pacmanOptions;
    private readonly Channel<PackageCacheRequest> _channel;

    public PersistentFileService(ILogger<PersistentFileService> logger, 
        IOptions<PersistentFileServiceOptions> options, IOptions<PacmanOptions> pacmanOptions)
    {
        _logger = logger;
        _options = options;
        _pacmanOptions = pacmanOptions;
        _channel = options.Value.Channel;
    }
    
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        //use retry policy
        try
        {
            await foreach (var persistPackageRequest in _options.Value.Channel.Reader.ReadAllAsync(stoppingToken))
            {
                _logger.LogInformation("File {FileName} has been processed", persistPackageRequest);
                if (persistPackageRequest.PackageStream.CanSeek)
                {
                    persistPackageRequest.PackageStream.Seek(0, SeekOrigin.Begin);
                }
                var filePath = Path.Combine(_pacmanOptions.Value.SaveDirectory, persistPackageRequest.PackageName);

                try
                {
                    await using var fileStream = new FileStream(filePath, new FileStreamOptions
                    {
                        Access = FileAccess.Read,
                        BufferSize = 4096,
                        Mode = FileMode.Create,
                        Options = FileOptions.WriteThrough,
                        PreallocationSize = 4096,
                        Share = FileShare.None,
                        UnixCreateMode = FILE_PERM
                    });
                    await persistPackageRequest.PackageStream.CopyToAsync(fileStream, stoppingToken);
                }
                //persist file
                catch(Exception)
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Exists)
                    {
                        _logger.LogInformation("File {FileName} still exists deleting", persistPackageRequest.PackageName);
                        fileInfo.Delete();
                    }
                }
                
                _logger.LogInformation("Saved {FileName} to {FilePath}", persistPackageRequest.PackageName, filePath);
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
            _options.Value.Channel.Writer.TryComplete();
        }
        
        _logger.LogInformation("Shut down background service safely");
    }

    public ValueTask EnqueueRequest(PackageCacheRequest request)
    {
        _logger.LogInformation("Adding {Request} to queue", request);
        return _options.Value.Channel.Writer.WriteAsync(request);
    }
}

public record PersistentFileServiceOptions
{
    public Channel<PackageCacheRequest> Channel { get; init; }
}

public record PackageCacheRequest
{
    public string PackageName { get; init; }
    public Stream PackageStream { get; init; }
}