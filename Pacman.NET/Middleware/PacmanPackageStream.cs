using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;

namespace Pacman.NET.Middleware;

public class PacmanPackageStream : Stream
{
    private readonly IHttpResponseBodyFeature _originalBodyFeature;
    private readonly FileStream _fileStream;
    private readonly ILogger<PacmanPackageStream> _logger;

    public PacmanPackageStream(IHttpResponseBodyFeature originalBodyFeature,
        FileStream fileStream,
        ILogger<PacmanPackageStream> logger)
    {
        _originalBodyFeature = originalBodyFeature;
        _fileStream = fileStream;
        _logger = logger;
    }


    public void DisableBuffering()
    {
        _originalBodyFeature.DisableBuffering();
    }

    public override void Flush()
    {
        _logger.LogInformation("Flushing {FileName}", _fileStream.Name);
        _fileStream.Flush();
        _originalBodyFeature.Stream.Flush();
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FlushAsync {FileName}", _fileStream.Name);
        await _fileStream.FlushAsync(cancellationToken);
        await _originalBodyFeature.Stream.FlushAsync(cancellationToken);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _originalBodyFeature.Stream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        _originalBodyFeature.Stream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _originalBodyFeature.Stream.Write(buffer, offset, count);
        _fileStream.Write(buffer, offset, count);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ctx)
    {
        var memoryBuffer = buffer.AsMemory(offset, count);
        await _fileStream.WriteAsync(memoryBuffer, ctx);
        await _originalBodyFeature.Stream.WriteAsync(memoryBuffer, ctx);
    }

    public override bool CanRead => _originalBodyFeature.Stream.CanRead;
    public override bool CanSeek => _originalBodyFeature.Stream.CanSeek;
    public override bool CanWrite => _originalBodyFeature.Stream.CanWrite;
    public override long Length => _originalBodyFeature.Stream.Length;

    public override long Position
    {
        get => _originalBodyFeature.Stream.Position;
        set => _originalBodyFeature.Stream.Position = value;
    }

    public override async ValueTask DisposeAsync()
    {
        await _fileStream.DisposeAsync();
        await _originalBodyFeature.Stream.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void Dispose(bool disposing)
    {
        _fileStream.Dispose();
        _originalBodyFeature.Stream.Dispose();
        base.Dispose(disposing);
    }
}