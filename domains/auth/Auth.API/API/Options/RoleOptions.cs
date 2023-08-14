using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class RoleOptions
{
    public const string Prefix = "Roles";

    [Required]
    public List<RoleConfiguration> RoleConfigurations { get; init; } = null!;
}

public class RoleConfiguration
{
    [Required(AllowEmptyStrings = false)]
    public string Key { get; init; } = null!;

    [Required(AllowEmptyStrings = false)]
    public string Name { get; init; } = null!;

    [Range(typeof(bool), "false", "true")]
    public bool IsDefault { get; set; } = false;

    [Range(typeof(bool), "false", "true")]
    public bool IsTransient { get; set; } = false;

    public List<Match> Matches { get; init; } = Array.Empty<Match>().ToList();

    public class Match
    {
        [Required(AllowEmptyStrings = false)]
        public string Property { get; init; } = null!;

        public string Value { get; init; } = string.Empty;

        [RegularExpression("exists|contains|equals", ErrorMessage = "Invalid Operator")]
        public string Operator { get; init; } = null!;
    }
}
