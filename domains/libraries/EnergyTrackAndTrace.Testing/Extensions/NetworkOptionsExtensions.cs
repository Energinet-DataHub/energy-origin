using System.IO;
using YamlDotNet.Serialization;

namespace EnergyTrackAndTrace.Testing.Extensions;

public static class NetworkOptionsExtensions
{
    public static string ToTempYamlFile(this NetworkOptions networkOptions)
    {
        var configFile = Path.GetTempFileName() + ".yaml";
        var serializer = new SerializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .Build();
        var yaml = serializer.Serialize(networkOptions);
        File.WriteAllText(configFile, yaml);
        return configFile;
    }
}
