using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;

namespace Pacman.NET.Middleware;

public class PacmanPackageBody : Stream, IHttpResponseBodyFeature
{
    private readonly HttpContext _context;
    private readonly string _filePath;
    private readonly IHttpResponseBodyFeature _innerBodyFeature;
    private bool _complete;
    private FileStream _fileStream;
    private PipeWriter? _pipeAdapter;

    public PacmanPackageBody(HttpContext context, IHttpResponseBodyFeature innerBodyFeature, string filePath)
    {
        _context = context;
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
        await _innerBodyFeature.Stream.DisposeAsync();
        await _fileStream.DisposeAsync();
    }


    public Stream Stream => _fileStream;


    public PipeWriter Writer => _pipeAdapter ??= PipeWriter.Create(Stream, new StreamPipeWriterOptions(leaveOpen: true));
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
        throw new NotImplementedException();
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override bool CanRead { get; }
    public override bool CanSeek { get; }
    public override bool CanWrite { get; }
    public override long Length { get; }
    public override long Position { get; set; }
}