namespace API.Helpers;

public static class Configuration
{
    // Token related
    private const string InternalTokenSecret = "INTERNALTOKENSECRET";
    private const string TokenExpiryTime = "TOKENEXPIRYTIME";


    private const string Scope = "SCOPE";
    private const string AmrValues = "AMRVALUES";


    // ------------------------------------------------
    public static string GetInternalTokenSecret()
    {
        var token = Environment.GetEnvironmentVariable(InternalTokenSecret) ?? throw new ArgumentNullException();

        return token;
    }

    public static int GetTokenExpiryTimeInDays()
    {
        var expiryTimeString = Environment.GetEnvironmentVariable(TokenExpiryTime) ?? throw new ArgumentNullException();

        var expiryTime = int.Parse(expiryTimeString);

        return expiryTime;
    }

    // ------------------------------------------------
    public static List<string> GetScopes()
    {
        var scope = Environment.GetEnvironmentVariable(Scope) ?? throw new ArgumentNullException();

        List<string> scopes = scope.Split(",").ToList();

        return scopes;
    }

    public static string GetAmrValues()
    {
        var amrValues = Environment.GetEnvironmentVariable(AmrValues) ?? throw new ArgumentNullException();

        return amrValues;
    }

    // ------------------------------------------------
}
