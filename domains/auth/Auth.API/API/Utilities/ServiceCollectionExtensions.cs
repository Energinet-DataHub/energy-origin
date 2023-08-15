using Microsoft.Extensions.Options;

namespace API.Utilities;

public static class ServiceCollectionExtensions
{
    public static OptionsBuilder<T> AttachOptions<T>(this IServiceCollection services) where T : class
    {
        var builder = services.AddOptions<T>();
        services.AddTransient(x => x.GetRequiredService<IOptions<T>>().Value);
        return builder;
    }
}

