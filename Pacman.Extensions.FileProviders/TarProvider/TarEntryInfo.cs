using System.Formats.Tar;

namespace Pacman.Extensions.FileProviders.TarProvider;

public record TarEntryInfo(TarEntry tarEntry) : IFileInfo
{
    public Stream CreateReadStream()
    {
        throw new NotImplementedException();
    }

    public bool Exists => true;
    public long Length => tarEntry.Length;

    public string PhysicalPath => tarEntry.EntryType is (TarEntryType.HardLink or TarEntryType.SymbolicLink)
        ? tarEntry.LinkName
        : tarEntry.Name;
    
    public string Name => tarEntry.Name;
    public DateTimeOffset LastModified => tarEntry.ModificationTime;
    public bool IsDirectory => tarEntry.EntryType is TarEntryType.Directory;
}