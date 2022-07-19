using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Oidc.Mock.Extensions;

public static class YamlExtensions
{
    public static void AddFromYamlFile<T>(this IServiceCollection services, string yamlFilePath) where T : class
    {
        var yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        using var reader = new StreamReader(yamlFilePath);
        var result = yamlDeserializer.Deserialize<T>(reader);

        if (result == null)
        {
            throw new Exception("Could not load YAML file");
        }

        services.AddSingleton<T>(_ => result);
    }
}
