using System.Text.Json;
using System.Text.Json.Serialization;
using Yarp.ReverseProxy.LoadBalancing;

namespace Pacman.NET.Services;

public class MirrorSyncService : BackgroundService, IProxyConfigProvider
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<MirrorSyncService> _logger;
    private readonly InMemoryConfigProvider _inMemoryConfigProvider;
    private readonly List<RouteConfig> _routes;
    private readonly List<ClusterConfig> _cluster = new();

    public MirrorSyncService(IHttpClientFactory clientFactory, ILogger<MirrorSyncService> logger, InMemoryConfigProvider inMemoryConfigProvider, IConfiguration config)
    {
        _clientFactory = clientFactory;
        _logger = logger;
        _inMemoryConfigProvider = inMemoryConfigProvider;
        var basePath = config["BaseAddress"] ?? "/archlinux";
        _routes = new List<RouteConfig>
        {
            new()
            {
                RouteId = "fileRoute",
                Match = new RouteMatch
                {
                    Methods = new []{"HEAD", "GET"},
                    Path = basePath + "/{repo}/os/{arch}/{fileName}",
                },
                ClusterId = "mirrorCluster",
                Transforms = new List<IReadOnlyDictionary<string, string>>()
                {
                    new Dictionary<string, string>()
                    {
                        ["PathPattern"] = "/{repo}/os/{arch}/{**fileName}"
                    }
                }
            }
        };
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
        var mirrors = jsonDoc.RootElement.GetProperty("urls").EnumerateArray();
        foreach (var mirror in mirrors)
        {
            var scoreProperty = mirror.GetProperty("score").GetRawText();
            double.TryParse(scoreProperty, out var score);
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
            .OrderBy(m => m.Score)
            .Take(10)
            .Select(m => new Uri(m.Url))
            .ToDictionary(m => m.Host, m => new DestinationConfig { Address = m.ToString() });


        _cluster.AddRange(new[]
        {
            new ClusterConfig
            {
                ClusterId = "mirrorCluster",
                LoadBalancingPolicy = LoadBalancingPolicies.RoundRobin,
                Destinations = sortedMirrors
            }
        });
        _inMemoryConfigProvider.Update(_routes, _cluster);
    }

    public IProxyConfig GetConfig() => _inMemoryConfigProvider.GetConfig();
}

public record MirrorInfo
{
    public required string Url { get; init; }
    public required string CountryCode { get; init; }
    public required double? Score { get; init; }
}