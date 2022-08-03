namespace API.Helpers;

public static class Configuration
{
    // Token related
    private const string InternalTokenSecret = "INTERNALTOKENSECRET";
    private const string TokenExpiryTime = "TOKENEXPIRYTIME";

    // OIDC Related
    private const string Scope = "SCOPE";
    private const string AmrValues = "AMRVALUES";
    private const string OidcUrl = "OIDCURL";
    private const string OidcClientId = "OIDCCLIENTID";
    private const string OidcClientSecret = "OIDCCLIENTSECRET";

    // Base url
    private const string BaseUrl = "BASEURL";


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
    public static string GetScopes()
    {
        var scope = Environment.GetEnvironmentVariable(Scope) ?? throw new ArgumentNullException();

        var combinedScopes = scope.Replace(",", "+").Replace(" ", "");

        return combinedScopes;
    }

    public static string GetAmrValues()
    {
        var amrValues = Environment.GetEnvironmentVariable(AmrValues) ?? throw new ArgumentNullException();

        return amrValues;
    }

    public static string GetOidcUrl()
    {
        var oidcUrl = Environment.GetEnvironmentVariable(OidcUrl) ?? throw new ArgumentNullException();

        return oidcUrl;
    }

    public static string GetOidcClientId()
    {
        var clientId = Environment.GetEnvironmentVariable(OidcClientId) ?? throw new ArgumentNullException();

        return clientId;
    }

    public static string GetOidcClientSecret()
    {
        var clientSecret = Environment.GetEnvironmentVariable(OidcClientSecret) ?? throw new ArgumentNullException();

        return clientSecret;
    }

    // ------------------------------------------------

    public static string GetBasetUrl()
    {
        var baseUrl = Environment.GetEnvironmentVariable(BaseUrl) ?? throw new ArgumentNullException();

        return baseUrl;
    }

}
