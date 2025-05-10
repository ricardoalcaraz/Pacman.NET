using System.Collections;
using Microsoft.Extensions.Primitives;

namespace Pacman.Extensions.FileProviders.TarProvider;

public class TarArchiveProvider : IFileProvider, IDirectoryContents
{
    public IFileInfo GetFileInfo(string subpath)
    {

        return new TarEntryInfo("");
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return this;
    }

    public IChangeToken Watch(string filter)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<IFileInfo> GetEnumerator()
    {
        yield return new TarEntryInfo("");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Exists { get; }
}