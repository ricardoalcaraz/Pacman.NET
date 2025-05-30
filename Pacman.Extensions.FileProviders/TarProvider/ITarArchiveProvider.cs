using System.Collections;
using System.Formats.Tar;
using Microsoft.Extensions.Primitives;

namespace Pacman.Extensions.FileProviders.TarProvider;

public class TarArchiveProvider(TarReader reader) : IDirectoryContents, IDisposable, IFileProvider
{
    private Dictionary<string, TarEntry> entries = new();
    
    public IFileInfo GetFileInfo(string subpath)
    {
        if (entries.TryGetValue(subpath, out var entry))
        {
            return new TarEntryInfo(entry);
        }

        return new NotFoundFileInfo(subpath);
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
            entries.Add(entry.Name, entry);
            yield return new TarEntryInfo(entry);
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

