using System.ComponentModel.DataAnnotations;

namespace EnergyOrigin.Setup.Cache;

public class RedisOptions
{
    public const string Prefix = "Redis";


    [Required]
    public string ConnectionString { get; set; } = string.Empty;
}
