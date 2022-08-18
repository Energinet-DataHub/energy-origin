using System;

namespace Tests.Resources
{
    public static class AddEnvironmentVariables
    {
        public static void EnvironmentVariables()
        {
            Environment.SetEnvironmentVariable("InternalTokenSecret", "INTERNALTOKENSECRET");
            Environment.SetEnvironmentVariable("TokenExpiryTime", "1");
            Environment.SetEnvironmentVariable("Scope", "SCOPE, SCOPE1");
            Environment.SetEnvironmentVariable("AmrValues", "AMRVALUES");
            Environment.SetEnvironmentVariable("AuthorityUrl", "https://pp.netseidbroker.dk/op");
            Environment.SetEnvironmentVariable("OidcClientId", "0a775a87-878c-4b83-abe3-ee29c720c3e7");
            Environment.SetEnvironmentVariable("OidcClientSecret", "rnlguc7CM/wmGSti4KCgCkWBQnfslYr0lMDZeIFsCJweROTROy2ajEigEaPQFl76Py6AVWnhYofl/0oiSAgdtg==");
            Environment.SetEnvironmentVariable("BaseUrl", "BASEURL");
            Environment.SetEnvironmentVariable("SecretKey", "mysmallkey123456");
        }
    }
}
