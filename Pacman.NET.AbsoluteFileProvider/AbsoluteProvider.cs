using Microsoft.Extensions.Primitives;

namespace Pacman.NET.AbsoluteFileProvider;

public class AbsoluteProvider : IFileProvider
{
    private readonly IFileProvider _fileProvider;

    public AbsoluteProvider(IFileProvider fileProvider)
    {
        _fileProvider = fileProvider;
    } 
    
    public AbsoluteProvider(string folderPath)
    {
        _fileProvider = new PhysicalFileProvider(folderPath);
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return new AbsoluteDirectoryContents(_fileProvider.GetDirectoryContents(subpath));
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        var fileInfo = _fileProvider.GetFileInfo(subpath);
        if (!fileInfo.Exists || string.IsNullOrWhiteSpace(fileInfo.PhysicalPath))
        {
            return new NotFoundFileInfo(subpath);
        }
        
        return fileInfo.IsDirectory ? fileInfo : new AbsoluteFileInfo(fileInfo.PhysicalPath);
    }

    public IChangeToken Watch(string filter)
    {
        throw new NotImplementedException();
    }
}