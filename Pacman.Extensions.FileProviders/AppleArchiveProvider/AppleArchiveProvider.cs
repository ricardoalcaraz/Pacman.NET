using Microsoft.Extensions.Primitives;

namespace Pacman.Extensions.FileProviders.AppleArchiveProvider;

public class AppleArchiveProvider : IFileProvider
{
    public IFileInfo GetFileInfo(string subpath)
    {
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