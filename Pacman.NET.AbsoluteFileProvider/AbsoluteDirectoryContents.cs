using System.Collections;

namespace Pacman.NET.AbsoluteFileProvider;

public class AbsoluteDirectoryContents : IDirectoryContents
{
    private readonly IDirectoryContents _directoryContents;

    public AbsoluteDirectoryContents(IDirectoryContents directoryContents)
    {
        _directoryContents = directoryContents;
    }

    public IEnumerator<IFileInfo> GetEnumerator()
    {
        foreach (var directoryContent in _directoryContents)
        {
            var file = directoryContent.IsDirectory || File.Exists(directoryContent.PhysicalPath) ? directoryContent : new AbsoluteFileInfo(directoryContent.PhysicalPath!);
            yield return file;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Exists => _directoryContents.Exists;
}