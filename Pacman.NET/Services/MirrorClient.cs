using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pacman.NET.Services;

public class MirrorClient
{
    private readonly HttpClient _client;
    private readonly ILogger<MirrorClient> _logger;

    public MirrorClient(HttpClient client, ILogger<MirrorClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<bool> IsMirrorUpToDate(string mirrorUrl, CancellationToken ctx)
    {
        var cutoffDate = DateTimeOffset.UtcNow - TimeSpan.FromHours(6);
        _logger.LogInformation("Checking if {Url} is up date date as of {Date}", mirrorUrl, cutoffDate);
        var cutoffUnixTime = cutoffDate.ToUnixTimeSeconds();
        try
        {
            var lastSyncResponse = (await _client.GetStringAsync($"{mirrorUrl}lastsync", ctx)).Trim();
            _logger.LogDebug("Received {Response} from {Url}", lastSyncResponse, mirrorUrl);
            var isValidUnixTime = long.TryParse(lastSyncResponse, out var lastSyncTime) && lastSyncTime > cutoffUnixTime;
            _logger.LogInformation("Is {Url} up to date: {Result}", mirrorUrl, isValidUnixTime);
            return isValidUnixTime;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Mirror lastsync request returned {StatusCode} for {Url}", ex.StatusCode, mirrorUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking if {Url} is up to date", mirrorUrl);
        }
        
        return false;
    }

    public async Task<JsonDocument> MirrorInfo(CancellationToken ctx)
    {
        var mirrorUri = new Uri("https://archlinux.org/mirrors/status/json");
        
        _logger.LogInformation("Requesting mirrors from {Url}", mirrorUri);
        
        var jsonDoc = await _client.GetFromJsonAsync<JsonDocument>(mirrorUri, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement
        }, ctx);

        return jsonDoc ?? throw new InvalidOperationException("Unable to parse Json Doc from url");
    }
}