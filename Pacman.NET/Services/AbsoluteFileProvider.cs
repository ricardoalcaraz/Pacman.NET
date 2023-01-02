using System.Collections;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;

namespace Pacman.NET.Services;

public class AbsoluteProvider : IFileProvider
{
    private readonly IFileProvider _fileProvider;


    public AbsoluteProvider(IFileProvider fileProvider)
    {
        _fileProvider = fileProvider;
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return new AbsoluteDirectoryContents(_fileProvider.GetDirectoryContents(subpath));
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        return new AbsoluteFileInfo(new FileInfo(subpath));
    }

    public IChangeToken Watch(string filter)
    {
        throw new NotImplementedException();
    }
}

public class AbsoluteDirectoryContents : IDirectoryContents
{
    private readonly IDirectoryContents _directoryContents;
    private readonly PhysicalFileProvider _fileProvider;

    public AbsoluteDirectoryContents(IDirectoryContents directoryContents)
    {
        _directoryContents = directoryContents;
    }

    public IEnumerator<IFileInfo> GetEnumerator()
    {
        foreach (var file in _directoryContents)
        {
            yield return new AbsoluteFileInfo(new FileInfo(file.PhysicalPath));
        }
    }

    public bool Exists => _directoryContents.Exists;
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class AbsoluteFileInfo : IFileInfo
{
    private readonly IFileInfo _actualFileInfo;
    private readonly FileInfo _fileInfo;

    public AbsoluteFileInfo(FileInfo filePath)
    {
        _fileInfo = filePath;
        if (filePath.Exists)
        {
            if (filePath.LinkTarget is not null && filePath.ResolveLinkTarget(true) is FileInfo actualFileInfo)
            {
                _actualFileInfo = new PhysicalFileInfo(actualFileInfo);
            }
        }

        _actualFileInfo ??= new PhysicalFileInfo(filePath);
    }

    public Stream CreateReadStream()
    {
        return _actualFileInfo.CreateReadStream();
    }

    public bool Exists => _actualFileInfo.Exists;
    public bool IsDirectory => _actualFileInfo.IsDirectory;
    public DateTimeOffset LastModified => _actualFileInfo.LastModified;
    public long Length => _actualFileInfo.Length;
    public string Name => _fileInfo.Name;
    public string? PhysicalPath => _actualFileInfo.PhysicalPath;
}