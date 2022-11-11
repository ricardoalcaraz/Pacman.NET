namespace Pacman.NET.Options
{
    public class BandwidthLimiterOptions
    {
        public required int MaxLimitInMB { get; init; }
    }

    public enum SizeUnit
    {
        Unknown,
        B,
        Mb,
        Gb
    }
}