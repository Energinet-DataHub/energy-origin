using API.Helpers;
using API.Models.Oidc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http.Extensions;


namespace API.Services;
public class TokenService
{
    private byte[] internalToken = Encoding.ASCII.GetBytes(Configuration.GetInternalTokenSecret());

    public string EncodeJwtToken(AuthState state)
    {
        var claims = new[]
        {
            new Claim("fe_url", state.FeUrl),
            new Claim("return_url", state.ReturnUrl),
            new Claim("terms_accepted", state.TermsAccepted.ToString()),
            new Claim("terms_version", state.TermsVersion),
            new Claim("id_token", state.IdToken),
            new Claim("tin", state.Tin),
            new Claim("identity_provider", state.IdentityProvider),
            new Claim("external_subject", state.ExternalSubject)
        };

        var key = new SymmetricSecurityKey(internalToken);
        var algorithm = SecurityAlgorithms.HmacSha256;

        var signingCredentials = new SigningCredentials(key, algorithm);

        var token = new JwtSecurityToken(
            claims: claims,
            signingCredentials: signingCredentials
            );

        var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

        return jwtToken;
    }

    public QueryBuilder CreateAuthorizationRedirectUrl(string responseType, string feUrl, AuthState state, string lang)
    {
        var query = new QueryBuilder();

        query.Add("response_type", responseType);
        query.Add("client_id", Configuration.GetOidcClientId());
        query.Add("redirect_uri", $"{feUrl}/api/auth/oidc/login/callback");
        query.Add("scope", Configuration.GetScopes());
        query.Add("state", EncodeJwtToken(state));
        query.Add("language", lang);

        return query;
    }
}
