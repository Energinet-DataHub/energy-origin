using Mock.Oidc.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Mock.Oidc;

public static class SettingsLoader
{
    public static void LoadSettings(this IServiceCollection services, IConfiguration configuration)
    {
        var yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        using var reader = new StreamReader(configuration["OidcFiles:UsersPath"]);
        var users = yamlDeserializer.Deserialize<UserDescriptor[]>(reader);

        services.AddSingleton<UserDescriptor[]>(_ => users);
    }
}