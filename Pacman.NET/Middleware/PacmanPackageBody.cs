using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;

namespace Pacman.NET.Middleware;

public class PacmanPackageBody : IHttpResponseBodyFeature
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
    }


    public void DisableBuffering()
    {
        _innerBodyFeature.DisableBuffering();
    }


    public Task StartAsync(CancellationToken cancellationToken = new())
    {
        _fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
        return Task.CompletedTask;
    }


    public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = new())
    {
        return _innerBodyFeature.SendFileAsync(path, offset, count, cancellationToken);
    }


    public async Task CompleteAsync()
    {
        await _fileStream.DisposeAsync();
    }


    public Stream Stream => _fileStream;


    public PipeWriter Writer => _pipeAdapter ??= PipeWriter.Create(Stream, new StreamPipeWriterOptions(leaveOpen: true));
}