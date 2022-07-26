namespace API.Helpers;

public static class Configuration
{
    private const string Scope = "SCOPE";

    public static List<String> GetScopes()
    {
        var scope = Environment.GetEnvironmentVariable(Scope) ?? throw new ArgumentNullException();

        List<string> scopes = scope.Split(",").ToList();

        return scopes;
    }
}
