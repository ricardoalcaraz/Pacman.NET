namespace Pacman.NET.Services;

public record MirrorInfo
{
    public required string Url { get; init; }
    public required string CountryCode { get; init; }
    public required double? Score { get; init; }
}