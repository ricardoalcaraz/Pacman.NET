using System.Collections;
using System.Formats.Tar;
using Microsoft.Extensions.Primitives;

namespace Pacman.Extensions.FileProviders.TarProvider;

public class TarArchiveProvider(TarReader reader) : IDirectoryContents, IDisposable, IFileProvider
{
    private readonly Dictionary<string, TarEntry> _entries = [];
    
    public IFileInfo GetFileInfo(string subpath)
    {
        if (_entries.TryGetValue(subpath, out var entry))
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
            _entries.Add(entry.Name, entry);
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

