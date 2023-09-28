using Microsoft.Extensions.Caching.Memory;

namespace API.TransferAgreementsAutomation;

public class AutomationCache
{
    public MemoryCache Cache { get; } = new(new MemoryCacheOptions());
}

public static class HealthEntries
{
    public const string Key = "Status";
    public const bool Healthy = true;
    public const bool Unhealthy = false;
}
