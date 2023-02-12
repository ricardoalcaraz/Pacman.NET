namespace Pacman.NET.AbsoluteFileProvider;

public class AbsoluteFileInfo : IFileInfo
{
    private readonly FileInfo _actualFileInfo;
    private readonly FileInfo _fileInfo;

    public AbsoluteFileInfo(string path)
    {
        _fileInfo = new FileInfo(path);
        var actualPath = File.ResolveLinkTarget(path, true)?.FullName;
        _actualFileInfo = string.IsNullOrWhiteSpace(actualPath) ? _fileInfo : new FileInfo(actualPath);
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

    public bool Exists => _fileInfo.Exists;
    public bool IsDirectory => false;
    public DateTimeOffset LastModified => _fileInfo.LastWriteTimeUtc;
    public long Length => _actualFileInfo.Length;
    public string Name => _fileInfo.Name;
    public string PhysicalPath => _actualFileInfo.FullName;
}