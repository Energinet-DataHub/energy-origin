namespace Oidc.Mock.Models;

public record User(string Name, string Description, string ImageUrl, Dictionary<string, object> IdToken, Dictionary<string, object> UserinfoToken);
