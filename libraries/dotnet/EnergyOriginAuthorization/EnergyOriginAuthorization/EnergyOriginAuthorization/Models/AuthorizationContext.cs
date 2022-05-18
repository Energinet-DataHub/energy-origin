using System.IdentityModel.Tokens.Jwt;

namespace EnergyOriginAuthorization
{
    public class AuthorizationContext
    {
        public string Actor { get; }
        public string Subject { get; }
        public AuthorizationContext(string subject, string actor)
        {
            Actor = actor;
            Subject = subject;
        }

        public static AuthorizationContext? decode(string token)
        {
            return decode(token, Enumerable.Empty<string>());
        }

        public static AuthorizationContext? decode(string token, IEnumerable<string> requiredScopes)
        {
            try
            {
                //Note - Key exchange has not yet been implemented so we do not yet validate the issuer signing.
                //See https://jasonwatmore.com/post/2022/01/19/net-6-create-and-validate-jwt-tokens-use-custom-jwt-middleware

                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

                var actor = jwt.Payload.First(x => x.Key == "actor").Value as string;
                var subject = jwt.Payload.First(x => x.Key == "subject").Value as string;
                var scopes = jwt.Payload.First(x => x.Key == "scope").Value as string ?? "";

                if (!string.IsNullOrWhiteSpace(actor) && !string.IsNullOrWhiteSpace(subject) && requiredScopes.All(it => scopes.Contains(it)))
                {
                    return new AuthorizationContext(subject, actor);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}