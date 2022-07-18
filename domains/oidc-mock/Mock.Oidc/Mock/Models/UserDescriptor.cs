namespace Mock.Oidc.Models;

public class UserDescriptor
{
    private Dictionary<string, object> _idToken;
    private Dictionary<string, object> _userinfoToken;

    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    public Dictionary<string, object> IdToken
    {
        get => _idToken;
        set => _idToken = EnsureCorrectTypeForNumbers(value);
    }

    public Dictionary<string, object> UserinfoToken
    {
        get => _userinfoToken;
        set => _userinfoToken = EnsureCorrectTypeForNumbers(value);
    }

    // This is needed to correct that YamlDotNet use string as type for numbers
    private static Dictionary<string, object> EnsureCorrectTypeForNumbers(Dictionary<string, object> input) =>
        input.ToDictionary(
            x => x.Key,
            x =>
            {
                if (long.TryParse((string?)x.Value, out long value))
                {
                    return value;
                }

                return x.Value;
            });
}