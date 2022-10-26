using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Issuer.Worker.MasterDataService;

public static class Startup
{
    public static void AddMasterDataService(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MockMasterDataOptions>(configuration.GetRequiredSection(MockMasterDataOptions.Prefix));
        services.AddSingleton<MockMasterDataCollection>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MockMasterDataOptions>>().Value;

            using var reader = new StreamReader(options.JsonFilePath);
            var json = reader.ReadToEnd();
            var result = JsonSerializer.Deserialize<MasterData[]>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
                });

            return new(result ?? Array.Empty<MasterData>());
        });
        services.AddSingleton<IMasterDataService, MockMasterDataService>();
    }
}
