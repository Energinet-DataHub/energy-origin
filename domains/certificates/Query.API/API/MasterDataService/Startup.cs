using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.MasterDataService.AuthService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API.MasterDataService;

public static class Startup
{
    public static void AddMasterDataService(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MockMasterDataOptions>(configuration.GetSection(MockMasterDataOptions.Prefix));

        services.AddHttpClient<AuthServiceClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<MockMasterDataOptions>>().Value;
            client.BaseAddress = new Uri(options.AuthServiceUrl);
        });
        services.AddSingleton<AuthServiceClientFactory>();

        services.AddSingleton<MockMasterDataCollection>(sp =>
        {
            try
            {
                var options = sp.GetRequiredService<IOptions<MockMasterDataOptions>>().Value;

                if (string.IsNullOrWhiteSpace(options.JsonFilePath))
                {
                    throw new Exception("No JsonFilePath");
                }

                using var reader = new StreamReader(options.JsonFilePath);
                var json = reader.ReadToEnd();
                var result = JsonSerializer.Deserialize<MockMasterData[]>(json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
                    });

                return new(result ?? Array.Empty<MockMasterData>());
            }
            catch (Exception e)
            {
                var logger = sp.GetService<ILogger<MockMasterDataCollection>>();
                logger?.LogWarning("Did not load mock master data. Exception: {exception}", e);
                return new(Array.Empty<MockMasterData>());
            }
        });

        services.AddSingleton<IMasterDataService, MockMasterDataService>();
    }
}
