using API.Authorization;
using Xunit;

namespace Tests.Infrastructure
{
    public class AuthorizeAttributeTests
    {
        [Fact]
        public void ValidateToken_GivenHardcodedToken_ValidatesToken()
        {
            var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhY3RvciI6IkpvaG4ifQ.jOJaJ-TwqnF9JtFanuD2k07F1AMGhTjZiVUDov_WSlA";

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt);

            Assert.True(result);
        }

        [Fact]
        public void ValidateToken_GivenTokenWithNameInsteadOfActor_ValidationFails()
        {
            var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1lIjoiSm9obiJ9.w0TEWOPQ8n0hQebQqgVy-hNm_GR3bz8eqNR_2yC6crI";

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt);

            Assert.False(result);
        }

        [Fact]
        public void ValidateToken_GivenTokenWithEmptyActor_ValidationFails()
        {
            var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhY3RvciI6IiJ9.7fDSZm3lBMtzktSMoW7MVEJlRj8wFzjzGSEeI_GW4VQ";

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt);

            Assert.False(result);
        }

        [Fact]
        public void ValidateToken_GivenTokenWithCapitalizedActor_ValidationFails()
        {
            var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJBY3RvciI6IkpvaG4ifQ.jOPHnjyGLpkloin6qFAZRnULZzYjiRsASw0Y1Zmoi14";

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt);

            Assert.False(result);
        }

        [Fact]
        public void ValidateToken_GivenTokenWithEmptyJWT_ValidationFails()
        {
            var jwt = "";

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt);

            Assert.False(result);
        }

        [Fact]
        public void ValidateToken_GivenMalformedToken_ValidationFails()
        {
            var jwt = "sdfkljhfdsjklghs";

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt);

            Assert.False(result);
        }

        [Fact]
        public void ValidateToken_GivenActorAsObject_ValidationFails()
        {
            var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhY3RvciI6eyJ0ZXN0IjoxfX0.C9ateG7hdoz1mv2AdfyNDNCOMZrIHun1bgCcBiEofGw";

            var authorize = new AuthorizeAttribute();

            var result = authorize.ValidateToken(jwt);

            Assert.False(result);
        }
    }
}