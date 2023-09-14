using System.Reflection.Metadata;
using Microsoft.Extensions.Caching.Memory;

namespace API.TransferAgreementsAutomation;

public class StatusCache
{
    public MemoryCache Cache { get; } = new(
        new MemoryCacheOptions
        {
            SizeLimit = 1024
        });


}

public static class CacheValues
{
    public const string Key = "Status";
    public const string Success = "Success";
    public const string Error = "Error";
}
