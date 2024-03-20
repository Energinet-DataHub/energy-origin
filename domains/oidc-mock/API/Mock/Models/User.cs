namespace Oidc.Mock.Models;

public record User(string Name, string Description, string ImageUrl, Dictionary<string, object> AccessToken, Dictionary<string, object> IdToken, Dictionary<string, object> UserinfoToken, bool isSelectable = true)
{
    public string? Subject
    {
        get
        {
            if (IdToken.TryGetValue("sub", out var subValue))
            {
                if (subValue is string subString)
                {
                    return subString;
                }
                else if (subValue is System.Text.Json.JsonElement jsonElement)
                {
                    return jsonElement.GetString();
                }
            }
            return null;
        }
    }
}
