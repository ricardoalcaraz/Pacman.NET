namespace Pacman.NET.Services;

public interface IMirrorService
{
    IAsyncEnumerable<string> MirrorUrlStream(CancellationToken ctx);
}