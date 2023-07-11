namespace API.Models.Entities;

public record RoleMatches
{
    public Guid? Id { get; init; }

    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
}
