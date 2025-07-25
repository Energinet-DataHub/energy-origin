using System;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace EnergyOrigin.Setup.Cache;

public static class EttCacheServiceCollectionExtensions
{
    public static IFusionCacheBuilder AddConfiguredFusionCache(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<FusionCacheOptions>? configureOptions = null,
        Action<FusionCacheEntryOptions>? configureEntryOptions = null)
    {
        services.AddOptions<RedisOptions>()
            .BindConfiguration(RedisOptions.Prefix)
            .ValidateDataAnnotations()
            .Validate(opt => !string.IsNullOrWhiteSpace(opt.ConnectionString), "Connection string must be provided")
            .ValidateOnStart();

        var redisOptions = configuration.GetSection(RedisOptions.Prefix).Get<RedisOptions>()
            is { ConnectionString: var cs } opts && !string.IsNullOrWhiteSpace(cs)
                ? opts
                : throw new OptionsValidationException(nameof(RedisOptions), typeof(RedisOptions), ["Redis:ConnectionString must be configured and non‑empty"]);

        var fusionCacheOptions = CreateDefaultFusionCacheOptions();
        configureOptions?.Invoke(fusionCacheOptions);

        var entryOptions = CreateDefaultEntryOptions();
        configureEntryOptions?.Invoke(entryOptions);

        return services
            .AddFusionCache()
            .WithOptions(fusionCacheOptions)
            .WithDefaultEntryOptions(entryOptions)
            .WithSerializer(new FusionCacheSystemTextJsonSerializer())
            .WithDistributedCache(new RedisCache(new RedisCacheOptions { Configuration = redisOptions.ConnectionString }))
            .WithBackplane(new RedisBackplane(new RedisBackplaneOptions { Configuration = redisOptions.ConnectionString }));
    }

    private static FusionCacheOptions CreateDefaultFusionCacheOptions() => new()
    {
        DistributedCacheCircuitBreakerDuration = TimeSpan.FromSeconds(2),
        FailSafeActivationLogLevel = LogLevel.Debug,
        SerializationErrorsLogLevel = LogLevel.Warning,
        DistributedCacheSyntheticTimeoutsLogLevel = LogLevel.Debug,
        DistributedCacheErrorsLogLevel = LogLevel.Error,
        FactorySyntheticTimeoutsLogLevel = LogLevel.Debug,
        FactoryErrorsLogLevel = LogLevel.Error
    };

    private static FusionCacheEntryOptions CreateDefaultEntryOptions() => new()
    {
        Duration = TimeSpan.FromMinutes(1),
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromHours(2),
        FailSafeThrottleDuration = TimeSpan.FromSeconds(30),
        FactorySoftTimeout = TimeSpan.FromMilliseconds(100),
        FactoryHardTimeout = TimeSpan.FromMilliseconds(1500),
        DistributedCacheSoftTimeout = TimeSpan.FromSeconds(1),
        DistributedCacheHardTimeout = TimeSpan.FromSeconds(2),
        AllowBackgroundDistributedCacheOperations = true,
        JitterMaxDuration = TimeSpan.FromSeconds(2)
    };
}
