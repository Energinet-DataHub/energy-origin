using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.MasterDataService.Clients;
using API.MasterDataService.MockInput;
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

            if (string.IsNullOrWhiteSpace(options.AuthServiceUrl))
            {
                throw new Exception("No AuthServiceUrl");
            }

            client.BaseAddress = new Uri(options.AuthServiceUrl);
        });
        services.AddSingleton<AuthServiceClientFactory>();

        services.AddSingleton<MasterDataMockInputCollection>(sp =>
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
                var result = JsonSerializer.Deserialize<MasterDataMockInput[]>(json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
                    });

                return new(result ?? Array.Empty<MasterDataMockInput>());
            }
            catch (Exception e)
            {
                var logger = sp.GetService<ILogger<MasterDataMockInputCollection>>();
                logger?.LogWarning("Did not load mock master data. Exception: {exception}", e);
                return new(Array.Empty<MasterDataMockInput>());
            }
        });

        services.AddSingleton<IMasterDataService, MockMasterDataService>();
    }
}
