using API.Values;

namespace API.Models.Response;

public class UserRolesResponse
{
    public required List<UserRoles> UserRoles { get; init; }
}

public class UserRoles
{
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
    public required Dictionary<string, string> Roles { get; init; }
}
