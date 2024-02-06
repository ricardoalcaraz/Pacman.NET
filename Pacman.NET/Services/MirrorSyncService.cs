using Yarp.ReverseProxy.LoadBalancing;

namespace Pacman.NET.Services;

public class MirrorSyncService : BackgroundService, IProxyConfigProvider
{
    private readonly ILogger<MirrorSyncService> _logger;
    private readonly IMirrorService _mirrorService;
    private readonly InMemoryConfigProvider _inMemoryConfigProvider;
    private readonly List<RouteConfig> _routes;
    private readonly List<ClusterConfig> _cluster = new();

    public MirrorSyncService(ILogger<MirrorSyncService> logger, 
        IProxyConfigProvider proxyConfigProvider, 
        IConfiguration config,
        IMirrorService mirrorService)
    {
        _logger = logger;
        _mirrorService = mirrorService;
        _inMemoryConfigProvider = proxyConfigProvider as InMemoryConfigProvider ?? throw new InvalidOperationException(); 
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
        var mirrorDestinations = new Dictionary<string, DestinationConfig>();

        await foreach (var mirror in _mirrorService.MirrorUrlStream(stoppingToken))
        {
            _logger.LogDebug("Received mirror {Url}", mirror);
            mirrorDestinations.Add(mirror, new DestinationConfig
            {
                Address = mirror
            });
            if (mirrorDestinations.Count > 6)
                break;
        }

        _cluster.AddRange(new[]
        {
            new ClusterConfig
            {
                ClusterId = "mirrorCluster",
                LoadBalancingPolicy = LoadBalancingPolicies.RoundRobin,
                Destinations = mirrorDestinations
            }
        });
        if (mirrorDestinations.Count != 0)
        {
            _logger.LogInformation("Discovered {Count} mirrors to use", mirrorDestinations.Count);
            _inMemoryConfigProvider.Update(_routes, _cluster);
        }
        else
        {
            _logger.LogWarning("No mirrors discovered for use");
        }
    }

    public IProxyConfig GetConfig() => _inMemoryConfigProvider.GetConfig();
}