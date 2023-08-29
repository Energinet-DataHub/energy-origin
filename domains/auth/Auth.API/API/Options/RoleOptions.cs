using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.IdentityModel.Tokens;

namespace API.Options;

[CustomValidation(typeof(RoleOptions), "Validate")]
public class RoleOptions
{
    public const string Prefix = "Roles";

    [Required]
    public List<RoleConfiguration> RoleConfigurations { get; init; } = Array.Empty<RoleConfiguration>().ToList();

    public static ValidationResult? Validate(RoleOptions options)
    {
        var keys = options.RoleConfigurations.Select(x => x.Key).Distinct();
        if (options.RoleConfigurations.Count != keys.Count())
        {
            return new ValidationResult("Role options contains duplicate keys");
        }
        foreach (var role in options.RoleConfigurations)
        {
            foreach (var key in role.Inherits)
            {
                if (keys.Contains(key) == false)
                {
                    return new ValidationResult($"Role {role.Key} contains unknown inherited role {key}");
                }
            }
        }
        var configurations = options.RoleConfigurations.ToDictionary(x => x.Key);
        foreach (var role in options.RoleConfigurations)
        {
            var inherited = FindInheritedRoles(configurations, role.Key, role.Inherits);
            if (inherited.Contains(role.Key))
            {
                return new ValidationResult($"Role {role.Key} will inherit itself");
            }
        }
        return ValidationResult.Success;
    }

    private static IEnumerable<string> FindInheritedRoles(Dictionary<string, RoleConfiguration> configurations, string forbidden, IEnumerable<string> roles)
    {
        if (roles.IsNullOrEmpty())
        {
            return Enumerable.Empty<string>();
        }
        var inherited = roles.Where(x => configurations[x].Inherits.IsNullOrEmpty() == false).SelectMany(x => configurations[x].Inherits);
        if (inherited.Contains(forbidden))
        {
            return new List<string>() { forbidden };
        }
        else
        {
            return roles.Concat(inherited).Concat(FindInheritedRoles(configurations, forbidden, inherited));
        }
    }
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
    public List<string> Inherits { get; init; } = Array.Empty<string>().ToList();
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
