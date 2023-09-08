namespace API.Models.Response;

public class UserRolesResponse
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public List<string> Roles { get; set; } = null!;
}
