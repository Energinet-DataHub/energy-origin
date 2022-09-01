using API.Configuration;
using Jose;
using Microsoft.Extensions.Options;

namespace API.Services
{
    public class JwkService : IJwkService
    {
        private readonly AuthOptions authOptions;
        private readonly ILogger<JwkService> logger;
        private readonly HttpClient httpClient;

        public JwkService(
            ILogger<JwkService> logger,
            IOptions<AuthOptions> authOptions,
            HttpClient httpClient
            )
        {
            this.logger = logger;
            this.authOptions = authOptions.Value;
            this.httpClient = httpClient;
        }

        public async Task<Jwk> GetJwkAsync()
        {
            var jwkResponse = await httpClient.GetAsync($"{authOptions.OidcUrl}/.well-known/openid-configuration/jwks");
            var jwkSet = JwkSet.FromJson(await jwkResponse.Content.ReadAsStringAsync(), new JsonMapper());
            var jwks = jwkSet.Keys.Single();

            return jwks;
        }
    }
}
