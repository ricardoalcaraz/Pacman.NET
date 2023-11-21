using System.Net;

namespace Pacman.NET.Middleware;

public class IpWhitelistMiddleware : IMiddleware
{
    private readonly ILogger<IpWhitelistMiddleware> _logger;
    private readonly IOptionsSnapshot<IpWhitelistOptions> _optionsSnapshot;


    public IpWhitelistMiddleware(ILogger<IpWhitelistMiddleware> logger, IOptionsSnapshot<IpWhitelistOptions> optionsSnapshot)
    {
        _logger = logger;
        _optionsSnapshot = optionsSnapshot;
    }
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var options = _optionsSnapshot.Value;
        var ip = context.Connection.RemoteIpAddress!;
        var isAllowedIp = IPAddress.IsLoopback(ip) || options.AllowedNetworks.Any(network => network.Contains(ip));
        if (isAllowedIp)
        {
            _logger.LogDebug("IP {Ip} is in allowed network", ip);
            await next(context);
        }
        else
        {
            _logger.LogInformation("Denied {Ip} because it is not in the allowed network", ip);
            context.Response.StatusCode = 403;
            if (!context.Response.HasStarted)
            {
                await context.Response.CompleteAsync();
                _logger.LogInformation("Completed request early");
            }
        }
    }
}

public record IpWhitelistOptions
{
    public List<IPNetwork> AllowedNetworks { get; set; } = new()
    {
        IPNetwork.Parse("192.168.1.0/24"),
        IPNetwork.Parse("45.29.157.145/29"),
        IPNetwork.Parse("99.98.255.106/32")
    };
}