using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;

namespace Pacman.NET.Middleware;

public class PacmanPackageBody : Stream, IHttpResponseBodyFeature
{
    private readonly string _filePath;
    private readonly IHttpResponseBodyFeature _innerBodyFeature;
    private bool _complete;
    private FileStream _fileStream;
    private PipeWriter? _pipeAdapter;

    public PacmanPackageBody(IHttpResponseBodyFeature innerBodyFeature, string filePath)
    {
        _innerBodyFeature = innerBodyFeature;
        _filePath = filePath;
        _fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
    }


    public void DisableBuffering()
    {
        _innerBodyFeature.DisableBuffering();
    }


    public async Task StartAsync(CancellationToken cancellationToken = new())
    {
        await _fileStream.FlushAsync(cancellationToken);
        await _innerBodyFeature.StartAsync(cancellationToken);
    }


    public async Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = new())
    {
        await _fileStream.FlushAsync(cancellationToken);
        await _innerBodyFeature.SendFileAsync(path, offset, count, cancellationToken);
    }


    public async Task CompleteAsync()
    {
        await _innerBodyFeature.Stream.FlushAsync();
        await _fileStream.FlushAsync();
    }

    public Stream Stream => _innerBodyFeature.Stream;
    public PipeWriter Writer => _innerBodyFeature.Writer;
    
    public override void Flush()
    {
        _fileStream.Flush();
        _innerBodyFeature.Stream.Flush();
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await _fileStream.FlushAsync(cancellationToken);
        await _innerBodyFeature.Stream.FlushAsync(cancellationToken);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        _fileStream.Read(buffer, offset, count);
        return Stream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return Stream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        Stream.SetLength(value);
        _fileStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Stream.Write(buffer, offset, count);
        _fileStream.Write(buffer, offset, count);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await _fileStream.WriteAsync(buffer, offset, count, cancellationToken);
        await Stream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override bool CanRead => Stream.CanRead;
    public override bool CanSeek => Stream.CanSeek;
    public override bool CanWrite => Stream.CanWrite;
    public override long Length => Stream.Length;
    public override long Position
    {
        get => Stream.Position;
        set => Stream.Position = value;
    }
}