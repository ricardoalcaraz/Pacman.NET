using System.Formats.Tar;
using Pacman.Extensions.FileProviders.TarProvider;

namespace Pacman.Extensions.FileProviders;

public class Class1
{
    public IFileInfo GetFileInfo(string subpath)
    {
        var tarProvider = new TarArchiveProvider(new TarReader(Stream.Null));
        foreach (var fileInfo in tarProvider)
        {
            return fileInfo;


        }
        return null;
    }
}