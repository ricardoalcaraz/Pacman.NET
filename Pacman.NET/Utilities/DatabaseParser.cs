using System.Collections;
using System.Formats.Tar;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Pacman.NET.Utilities;

public class DatabaseParser
{
}

public class PacmanPackageInfo : IFileInfo
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

public class DatabaseFileProvider : IFileProvider
{
    public IFileInfo GetFileInfo(string subpath)
    {
        var tarStream = Stream.Null;
        TarReader reader = new TarReader(tarStream);
        throw new NotImplementedException();
    }


    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        throw new NotImplementedException();
    }


    public IChangeToken Watch(string filter)
    {
        throw new NotImplementedException();
    }
}

public class DatabaseDirectoryContents : IDirectoryContents
{
    public IEnumerator<IFileInfo> GetEnumerator()
    {
        throw new NotImplementedException();
    }


    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }


    public bool Exists { get; }
} 