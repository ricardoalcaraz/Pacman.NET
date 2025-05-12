using System.Collections;
using System.Formats.Tar;
using Microsoft.Extensions.Primitives;

namespace Pacman.Extensions.FileProviders.TarProvider;

public class TarArchiveProvider(TarReader reader) : IDirectoryContents, IDisposable, IFileProvider
{
    public IFileInfo GetFileInfo(string subpath)
    {

        return new TarEntryInfo("");
    }

    

    public IChangeToken Watch(string filter)
    {
        throw new NotImplementedException();
    }

    public IDirectoryContents GetDirectoryContents(string subpath) => this;

    public IEnumerator<IFileInfo> GetEnumerator()
    {
        while (reader.GetNextEntry() is TarEntry entry)
        {
            yield return new TarEntryInfo(entry.Name);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Exists { get; }

    public void Dispose()
    {
        reader.Dispose();
    }
}

