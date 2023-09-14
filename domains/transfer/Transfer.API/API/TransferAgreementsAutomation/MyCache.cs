using System.Reflection.Metadata;
using Microsoft.Extensions.Caching.Memory;

namespace API.TransferAgreementsAutomation;

public class MyCache
{
    public MemoryCache Cache { get; } = new MemoryCache(
        new MemoryCacheOptions
        {
            SizeLimit = 1024
        });


}

public static class CacheValues
{
    public static string Key = "Status";
    public static string Success = "Success";
    public static string Error = "Error";
}
