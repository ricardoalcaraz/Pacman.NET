using System.Runtime.CompilerServices;

namespace Pacman.NET.Services;

public class RemoteMirrorService : IMirrorService
{
    private readonly ILogger<RemoteMirrorService> _logger;
    private readonly MirrorClient _mirrorClient;

    public RemoteMirrorService(ILogger<RemoteMirrorService> logger, MirrorClient mirrorClient)
    {
        _logger = logger;
        _mirrorClient = mirrorClient;
    }

    public async IAsyncEnumerable<string> MirrorUrlStream([EnumeratorCancellation] CancellationToken ctx)
    {
        var jsonDoc = await _mirrorClient.MirrorInfo(ctx);
        
        _logger.LogDebug("Received following response: {Json}", jsonDoc);
        var mirrors = jsonDoc.RootElement.GetProperty("urls").EnumerateArray();

        var mirrorList = new List<MirrorInfo>();
        foreach (var mirror in mirrors)
        {
            var isScore = double.TryParse(mirror.GetProperty("score").GetRawText(), out var score);
            var isActive = mirror.GetProperty("active").GetBoolean();
            var isHttps = mirror.GetProperty("protocol").GetString() == "https";

            if (isActive && isHttps && score > 0)
            {
                var mirrorInfo = new MirrorInfo
                {
                    Url = mirror.GetProperty("url").GetString() ?? string.Empty,
                    CountryCode = mirror.GetProperty("country_code").GetString() ?? string.Empty,
                    Score = score
                };
                
                mirrorList.Add(mirrorInfo);
            }
        }

        var sortedMirrors = mirrorList
            .Where(m => m.CountryCode == "US")
            .Where(m => !string.IsNullOrWhiteSpace(m.Url))
            .OrderBy(m => m.Score);
        
        foreach (var mirror in sortedMirrors)
        {
            if (await _mirrorClient.IsMirrorUpToDate(mirror.Url, ctx))
            {
                yield return mirror.Url;
            }
        }
    }
}