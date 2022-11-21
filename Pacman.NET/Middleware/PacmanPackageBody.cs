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

    public PacmanPackageBody(IHttpResponseBodyFeature innerBodyFeature, FileStream fileStream)
    {
        _innerBodyFeature = innerBodyFeature;
        _fileStream = fileStream;
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

    public Stream Stream => this;
    public PipeWriter Writer => PipeWriter.Create(_innerBodyFeature.Stream, new StreamPipeWriterOptions(leaveOpen: true));
    
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
        return _innerBodyFeature.Stream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _innerBodyFeature.Stream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _innerBodyFeature.Stream.SetLength(value);
        _fileStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _innerBodyFeature.Stream.Write(buffer, offset, count);
        _fileStream.Write(buffer, offset, count);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ctx)
    {
        var memoryBuffer = buffer.AsMemory(offset, count);
        await _fileStream.WriteAsync(memoryBuffer, ctx);
        await _innerBodyFeature.Stream.WriteAsync(memoryBuffer, ctx);
    }

    public override bool CanRead => _innerBodyFeature.Stream.CanRead;
    public override bool CanSeek => _innerBodyFeature.Stream.CanSeek;
    public override bool CanWrite => _innerBodyFeature.Stream.CanWrite;
    public override long Length => _innerBodyFeature.Stream.Length;
    public override long Position
    {
        get => _innerBodyFeature.Stream.Position;
        set => _innerBodyFeature.Stream.Position = value;
    }
}