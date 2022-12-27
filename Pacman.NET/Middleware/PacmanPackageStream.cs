using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace Pacman.NET.Middleware;

public class PacmanPackageStream : Stream, IHttpResponseBodyFeature
{
    private PipeWriter? _pipeAdapter;
    private readonly IHttpResponseBodyFeature _originalBodyFeature;
    private readonly string _fileName = Path.GetTempFileName();
    private readonly FileStream _fileStream;
    private readonly ILogger<PacmanPackageStream> _logger = NullLogger<PacmanPackageStream>.Instance;

    public PacmanPackageStream(IHttpResponseBodyFeature originalBodyFeature)
    {
        _originalBodyFeature = originalBodyFeature;
        _fileStream = new FileStream(_fileName, new FileStreamOptions
        {
            Access = FileAccess.Write,
            BufferSize = 0,
            Mode = FileMode.Create,
            Options = FileOptions.SequentialScan,
            Share = FileShare.Delete
        });
    }


    public void DisableBuffering()
    {
        _originalBodyFeature.DisableBuffering();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _fileStream.FlushAsync(cancellationToken);
        await _originalBodyFeature.StartAsync(cancellationToken);
    }

    public async Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = new CancellationToken())
    {
        await _originalBodyFeature.SendFileAsync(path, offset, count, cancellationToken);
    }
    public string GetFileName()
    {
        return _fileName;
    }

    public FileInfo PackageFileInfo() => new(_fileName);
    public async Task CompleteAsync()
    {
        await _fileStream.FlushAsync();
        await _originalBodyFeature.CompleteAsync();
    }

    public PipeWriter Writer => _pipeAdapter ??= PipeWriter.Create(Stream, new StreamPipeWriterOptions(leaveOpen: true));

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
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
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

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => _fileStream.CanWrite;
    public override long Length
    {
        get { throw new NotSupportedException(); }
    }

    public override long Position
    {
        get { throw new NotSupportedException(); }
        set { throw new NotSupportedException(); }
    }

    public Stream Stream => this;

    public override async ValueTask DisposeAsync()
    {
        await _fileStream.DisposeAsync();
        await _originalBodyFeature.Stream.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void Dispose(bool disposing)
    {
        _fileStream?.Dispose();
        _originalBodyFeature.Stream.Dispose();
        base.Dispose(disposing);
    }

    public FileStream GetTempFile()
    {
        return File.Open(_fileName, FileMode.Open, FileAccess.Read, FileShare.Delete);
    }
}