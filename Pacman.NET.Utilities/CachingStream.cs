using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Pacman.NET.Utilities;

public class CachingStream : Stream, IHttpResponseBodyFeature
{
    private PipeWriter? _pipeAdapter;
    private readonly IHttpResponseBodyFeature _originalBodyFeature;
    private readonly FileStream _fileStream;
    private volatile bool _isDisposed;

    public CachingStream(IHttpResponseBodyFeature originalBodyFeature, FileStream fileStream)
    {
        _originalBodyFeature = originalBodyFeature;
        _fileStream = fileStream;
    }


    public Stream Stream => this;

    public PipeWriter Writer => _pipeAdapter ??= PipeWriter.Create(Stream, new StreamPipeWriterOptions(leaveOpen: true));
    
    public void DisableBuffering() => _originalBodyFeature.DisableBuffering();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _fileStream.FlushAsync(cancellationToken);
        await _originalBodyFeature.StartAsync(cancellationToken);
    }

    
    public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
    {
        //check if the file can even be opened
        if (File.Exists(path))
        {
            return _originalBodyFeature.SendFileAsync(path, offset, count, cancellationToken);
        }

        return Task.CompletedTask;
    }


    public async Task CompleteAsync()
    {
        await _fileStream.FlushAsync();
        await _originalBodyFeature.CompleteAsync();
    }
    
    public override void Flush()
    {
        _fileStream.Flush();
        _originalBodyFeature.Stream.Flush();
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
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

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ctx = default)
    {
        await _fileStream.WriteAsync(buffer, ctx);
        await _originalBodyFeature.Stream.WriteAsync(buffer, ctx);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ctx)
    {
        await WriteAsync(buffer.AsMemory()[offset..count], ctx);
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

    public override async ValueTask DisposeAsync()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            await _fileStream.DisposeAsync();
            await _originalBodyFeature.Stream.DisposeAsync();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            _fileStream.Dispose();
            _originalBodyFeature.Stream.Dispose();
        }
    }

}