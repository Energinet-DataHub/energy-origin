using Microsoft.Extensions.Caching.Memory;

namespace API.TransferAgreementsAutomation;

public class StatusCache
{
    public MemoryCache Cache { get; } = new(new MemoryCacheOptions());
}

public static class CacheValues
{
    public const string Key = "Status";
    public const bool Healthy = true;
    public const bool Unhealthy = false;
}
