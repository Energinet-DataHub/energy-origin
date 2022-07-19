namespace Oidc.Mock.Models;

public class UserDescriptor
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    public Dictionary<string, object>? IdToken { get; set; }

    public Dictionary<string, object>? UserinfoToken { get; set; }
}
