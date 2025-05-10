namespace Pacman.Extensions.FileProviders.TarProvider;

public record TarEntryInfo(string Empty) : IFileInfo
{
    public Stream CreateReadStream()
    {
        throw new NotImplementedException();
    }

    public bool Exists { get; }
    public long Length { get; }
    public string? PhysicalPath { get; }
    public string Name { get; }
    public DateTimeOffset LastModified { get; }
    public bool IsDirectory { get; }
}