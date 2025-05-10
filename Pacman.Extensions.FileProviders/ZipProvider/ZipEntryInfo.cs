namespace Pacman.Extensions.FileProviders.ZipProvider;

public record ZipEntryInfo(string path):IFileInfo
{
    public Stream CreateReadStream()
    {
        return Stream.Null;
    }

    public bool Exists { get; }
    public long Length { get; }
    public string? PhysicalPath { get; }
    public string Name { get; }
    public DateTimeOffset LastModified { get; }
    public bool IsDirectory { get; }
}