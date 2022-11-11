using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pacman.NET.Services;

public class MirrorSyncService : BackgroundService
{
    private readonly IHttpClientFactory _clientFactory;
    private List<MirrorInfo> _fastestMirrors;


    public MirrorSyncService(IHttpClientFactory clientFactory, List<MirrorInfo> fastestMirrors)
    {
        _clientFactory = clientFactory;
        _fastestMirrors = fastestMirrors;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mirrorUri = new Uri("https://archlinux.org/mirrors/status/json/");
        using var httpClient = _clientFactory.CreateClient();
        var jsonDoc = await httpClient.GetFromJsonAsync<JsonDocument>(mirrorUri, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement
        }, stoppingToken);


        var lastCheck = jsonDoc.RootElement.GetProperty("last_check").GetString();

        var mirrorList = new List<MirrorInfo>();
        var urls = jsonDoc.RootElement.GetProperty("urls");
        foreach (var mirror in urls.EnumerateArray())
        {
            var scoreProperty = mirror.GetProperty("score").GetRawText();
            double.TryParse(scoreProperty, out var score);
            var isActive = mirror.GetProperty("active").GetBoolean();


            if (isActive && score > 0)
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

        _fastestMirrors = mirrorList
            .Where(m => m.CountryCode == "US")
            .Where(m => !string.IsNullOrWhiteSpace(m.Url))
            .OrderBy(m => m.Score)
            .Take(10)
            .ToList();
    }


    public IEnumerable<MirrorInfo> GetNextMirror()
    {
        var index = 0;
        while (true)
        {
            yield return _fastestMirrors[Math.Clamp(index, 0, _fastestMirrors.Count)];

            if (index > _fastestMirrors.Count)
            {
                index = 0;
            }
            else
            {
                Interlocked.Increment(ref index);
            }
        }
    }
}

public record MirrorInfo
{
    public required string Url { get; init; }
    public required string CountryCode { get; init; }
    public required double? Score { get; init; }
}