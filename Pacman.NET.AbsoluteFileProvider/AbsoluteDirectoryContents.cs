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
        foreach (var file in _directoryContents)
        {
            var fil = file.IsDirectory || string.IsNullOrWhiteSpace(file.PhysicalPath) ? file : new AbsoluteFileInfo(file.PhysicalPath);
            yield return fil;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Exists => _directoryContents.Exists;
}