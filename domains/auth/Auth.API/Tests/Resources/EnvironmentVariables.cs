using System;

namespace Tests.Resources
{
    public static class EnvironmentVariable
    {
        public static void EnvironmentVariables()
        {
            Environment.SetEnvironmentVariable("InternalTokenSecret", "INTERNALTOKENSECRET");
            Environment.SetEnvironmentVariable("TokenExpiryTime", "1");
            Environment.SetEnvironmentVariable("Scope", "SCOPE, SCOPE1");
            Environment.SetEnvironmentVariable("AmrValues", "AMRVALUES");
            Environment.SetEnvironmentVariable("OidcUrl", "OIDCURL");
            Environment.SetEnvironmentVariable("OidcClientId", "OIDCCLIENTID");
            Environment.SetEnvironmentVariable("OidcClientSecret", "OIDCCLIENTSECRET");
        }
    }
}
