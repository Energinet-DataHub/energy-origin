namespace Oidc.Mock.Models;

public record User(string Name, bool IsSelectable, string Description, string ImageUrl, Dictionary<string, object> AccessToken, Dictionary<string, object> IdToken, Dictionary<string, object> UserinfoToken);
