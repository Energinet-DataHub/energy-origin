using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.Net.Http;
using System.Text.Json;
using API.Configuration;
using API.Controllers;
using API.Controllers.dto;
using API.Errors;
using API.Models;
using API.Repository;
using API.Services;
using API.Services.OidcProviders;
using API.TokenStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Categories;
using API.Helpers;
using FluentValidation;

namespace API.Controllers;

public class TestLoginController
{
    private readonly Mock<IOidcService> mockSignaturGruppen = new();
    private readonly Mock<IOptions<AuthOptions>> authOptionsMock = new();
    private readonly Mock<ITokenStorage> tokenStorage = new();
    private readonly Mock<ICryptography> cryptography = new();
    private readonly Mock<IValidator<OidcCallbackParams>> validator = new();

    private readonly LoginController loginController;

    public TestLoginController()
    {
        authOptionsMock.Setup(x => x.Value).Returns(new AuthOptions
        {
            CookieName = "Authorization",
        });

        loginController = new LoginController(
            mockSignaturGruppen.Object,
            validator.Object,
            authOptionsMock.Object,
            cryptography.Object
        )
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }


    [Fact]
    public async Task LoginCallbackTokenDecode()
    {

        var token = new OidcTokenResponse { IdToken = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjA0ODA1OEJCNTlGNEQzMDA3MDQ1ODk2RkQ0ODhDRTgxRjRFQjQ5MjMiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL3BwLm5ldHNlaWRicm9rZXIuZGsvb3AiLCJuYmYiOjE2NjE4NDIzNDcsImlhdCI6MTY2MTg0MjM0NywiZXhwIjoxNjYxODQyNjQ3LCJhdWQiOiIwYTc3NWE4Ny04NzhjLTRiODMtYWJlMy1lZTI5YzcyMGMzZTciLCJhbXIiOlsiY29kZV9hcHAiXSwiYXRfaGFzaCI6ImR2TmY5blpVQVotY2I2UjdhZUswU1EiLCJzdWIiOiI5MjM5OTQxMS0xYjk4LTRhMDEtOTIyMS0wNWJhZWJhZDI2NTkiLCJhdXRoX3RpbWUiOjE2NjE4NDIzMDYsImlkcCI6Im1pdGlkIiwiYWNyIjoiaHR0cHM6Ly9kYXRhLmdvdi5kay9jb25jZXB0L2NvcmUvbnNpcy9TdWJzdGFudGlhbCIsIm5lYl9zaWQiOiI5NDZmYjRlZi02ZDlhLTQ0ZjItYjE5Yi00NjRkMTY0MTZjYjciLCJsb2EiOiJodHRwczovL2RhdGEuZ292LmRrL2NvbmNlcHQvY29yZS9uc2lzL1N1YnN0YW50aWFsIiwiYWFsIjoiaHR0cHM6Ly9kYXRhLmdvdi5kay9jb25jZXB0L2NvcmUvbnNpcy9TdWJzdGFudGlhbCIsImlhbCI6Imh0dHBzOi8vZGF0YS5nb3YuZGsvY29uY2VwdC9jb3JlL25zaXMvU3Vic3RhbnRpYWwiLCJpZGVudGl0eV90eXBlIjoicHJpdmF0ZSIsInRyYW5zYWN0aW9uX2lkIjoiYjYyZGZmYzgtYThmMi00MWExLTllNTItOWRiNzFhYTZjMWYwIiwiaWRwX3RyYW5zYWN0aW9uX2lkIjoiODFiOTM3M2EtNGU3YS00MTIxLWFkMTctN2IzNDgxYjFmNjZjIiwic2Vzc2lvbl9leHBpcnkiOiIxNjYxODU4NTAzIn0.qptpC_y946lkOqABNVW - pRUOKTu1rx3iUkxrKhtydG2bVpshBAgmq2gpwZ5KtpJXfpdVbAFaw2JdbSwMG6dnU14xORJdUqnYkzSOaLuALJykf2CK3wzxNFz4LJ_pFIvrh52q0YbUYC5JBIvExU6ugffHhunet1rd8UcLjDjveGAsbFLi8T5IXWBzMDtdnCUEqELa4GzQBQsKKcnmHy8MbpEPD - L9K_HlljN_rAYUbZkIevZCgLkavqt81n2RZtih75qEEmofvAA6bNaYgkd_XlNiTWdYG53zu4Nyc5EUSJqI1eS - P_8TbnNIrFld3L3QK8Tv1VVNVbAbwoeqiyvVNw" };

        var tt = loginController.ClaimToken(token);

        //Assert.Equal(expectedExpiredCookie, loginController.HttpContext.Response.GetTypedHeaders().SetCookie.Single().ToString());
        //return Task.CompletedTask;
    }
}
