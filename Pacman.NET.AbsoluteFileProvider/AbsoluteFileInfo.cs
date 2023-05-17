namespace Pacman.NET.AbsoluteFileProvider;

public class AbsoluteFileInfo : IFileInfo
{
    private readonly FileInfo _actualFileInfo;

    public AbsoluteFileInfo(string path)
    {
        var actualPath = File.ResolveLinkTarget(path, true)?.FullName;
        _actualFileInfo = new FileInfo(actualPath ?? path);
        Name = Path.GetFileName(path);
        IsDirectory = Directory.Exists(path);
    }

    public Stream CreateReadStream()
    {
        // We are setting buffer size to 1 to prevent FileStream from allocating it's internal buffer
        // 0 causes constructor to throw
        const int BUFFER_SIZE = 1;
        return new FileStream(
            PhysicalPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            BUFFER_SIZE,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    public bool Exists => _actualFileInfo.Exists;
    public bool IsDirectory { get; }
    public DateTimeOffset LastModified => _actualFileInfo.LastWriteTimeUtc;
    public long Length => _actualFileInfo.Length;
    public string Name { get; }
    public string PhysicalPath => _actualFileInfo.FullName;
}