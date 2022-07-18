using System.Linq;
using Xunit;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System;
using System.Text;
using System.Collections.Generic;

namespace EnergyOriginAuthorization.Tests
{
    public class AuthorizeAttributeTests
    {
        private string GenerateToken(
            string scope = "",
            string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f",
            string subject = "bdcb3287-3dd3-44cd-8423-1f94437648cc",
            string actorKey = "actor")
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("TESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTEST");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("subject", subject), new Claim("scope", scope), new Claim(actorKey, actor) }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        [Fact]
        public void ValidateToken_GivenValidToken_ValidatesToken()
        {
            var jwt = GenerateToken();

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt, Enumerable.Empty<string>());

            Assert.True(result);
        }

        [Fact]
        public void ValidateToken_GivenTokenWithNameInsteadOfActor_ValidationFails()
        {
            var jwt = GenerateToken(actorKey: "name");

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt, Enumerable.Empty<string>());

            Assert.False(result);
        }

        [Fact]
        public void ValidateToken_GivenTokenWithEmptyActor_ValidationFails()
        {
            var jwt = GenerateToken(actor: "");

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt, Enumerable.Empty<string>());

            Assert.False(result);
        }

        [Fact]
        public void ValidateToken_GivenTokenWithCapitalizedActor_ValidationFails()
        {
            var jwt = GenerateToken(actorKey: "Actor");

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt, Enumerable.Empty<string>());

            Assert.False(result);
        }

        [Fact]
        public void ValidateToken_GivenTokenWithEmptyJWT_ValidationFails()
        {
            var jwt = "";

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt, Enumerable.Empty<string>());

            Assert.False(result);
        }

        [Fact]
        public void ValidateToken_GivenMalformedToken_ValidationFails()
        {
            var jwt = "sdfkljhfdsjklghs";

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt, Enumerable.Empty<string>());

            Assert.False(result);
        }

        [Fact]
        public void ValidateToken_GivenAValidAndPresentScope_ValidatesToken()
        {
            var jwt = GenerateToken(scope: "needed-scope");

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt, new List<string> { "needed-scope" });

            Assert.True(result);
        }

        [Fact]
        public void ValidateToken_GivenAWrongScope_ValidationFails()
        {
            var jwt = GenerateToken(scope: "some-other-scope");

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt, new List<string> { "needed-scope" });

            Assert.False(result);
        }

        [Fact]
        public void ValidateToken_GivenAWrongCasedScope_ValidationFails()
        {
            var jwt = GenerateToken(scope: "NEEDED-SCOPE");

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt, new List<string> { "needed-scope" });

            Assert.False(result);
        }

        [Fact]
        public void ValidateToken_GivenNoScopeWhenScopeIsRequired_ValidationFails()
        {
            var jwt = GenerateToken(scope: "");

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt, new List<string> { "needed-scope" });

            Assert.False(result);
        }


        [Fact]
        public void ValidateToken_GivenAScopeWhenNoScopeIsRequired_ValidatesToken()
        {
            var jwt = GenerateToken(scope: "needed-scope");

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt, Enumerable.Empty<string>());

            Assert.True(result);
        }

        [Fact]
        public void ValidateToken_GivenAScopeWhenMultipleScopesIsRequired_ValidationFails()
        {
            var jwt = GenerateToken(scope: "needed-scope");

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt, new List<string> { "needed-scope", "other-needed-scope" });

            Assert.False(result);
        }

        [Fact]
        public void ValidateToken_GivenMultipleScopeWhenASingleScopeIsRequired_ValidatesToken()
        {
            var jwt = GenerateToken(scope: "needed-scope some-other-scope");

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt, new List<string> { "needed-scope" });

            Assert.True(result);
        }


        [Fact]
        public void ValidateToken_GivenMultipleScopeWhenMultipleScopesIsRequired_ValidatesToken()
        {
            var jwt = GenerateToken(scope: "needed-scope other-needed-scope");

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt, new List<string> { "needed-scope", "other-needed-scope" });

            Assert.True(result);
        }
    }
}
