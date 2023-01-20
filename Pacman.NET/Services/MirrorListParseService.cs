using System.Runtime.CompilerServices;

namespace Pacman.NET.Services;

public class MirrorListParseService : IMirrorService
{
    private readonly PacmanConfigParser _configParser;
    private readonly ILogger<MirrorListParseService> _logger;
    private readonly IOptions<PacmanOptions> _options;
    private readonly MirrorClient _mirrorClient;

    public MirrorListParseService(PacmanConfigParser configParser, 
        ILogger<MirrorListParseService> logger, 
        IOptions<PacmanOptions> options, 
        MirrorClient mirrorClient)
    {
        _configParser = configParser;
        _logger = logger;
        _options = options;
        _mirrorClient = mirrorClient;
    }


    public async IAsyncEnumerable<string> MirrorUrlStream([EnumeratorCancellation] CancellationToken ctx)
    {
        var mirrorUri = _options.Value.MirrorUrl;
        _logger.LogInformation("Retrieving mirrors from file at {Dir}", mirrorUri);

        await foreach (var mirror in _configParser.ParseMirrorlist(mirrorUri, ctx))
        {
            var mirrorUrl = new string(mirror.TakeWhile(c => c != '$').ToArray());
            if (await _mirrorClient.IsMirrorUpToDate(mirrorUrl, ctx))
            {
                _logger.LogDebug("Using up to date mirror {Url}", mirrorUrl);
                yield return mirrorUrl;
            }
            else
            {
                _logger.LogWarning("Ignoring {Url} because it is out of date", mirrorUrl);
            }
        }
    }
}