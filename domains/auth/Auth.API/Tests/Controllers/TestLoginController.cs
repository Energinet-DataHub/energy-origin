using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using API.Configuration;
using API.Controllers.dto;
using API.Models;
using API.Orchestrator;
using API.Repository;
using API.Services;
using API.Services.OidcProviders;
using API.Utilities;
using EnergyOriginEventStore.EventStore.Memory;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace API.Controllers;

public class TestLoginController
{
    private readonly Mock<IOidcService> mockSignaturGruppen = new();
    private readonly Mock<IOptions<AuthOptions>> authOptionsMock = new();
    private readonly Mock<ITokenStorage> tokenStorage = new();
    private readonly Mock<ICryptographyFactory> cryptographyFactory = new();
    private readonly Mock<IValidator<OidcCallbackParams>> validator = new();
    private readonly Mock<IJwkService> jwkService = new();
    private readonly MemoryEventStore eventStore = new();
    private readonly Mock<IUserStorage> userStorage = new();
    private readonly Mock<ICompanyStorage> companyStorage = new();
    private readonly Mock<IOrchestrator> orchestrator = new();
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
            cryptographyFactory.Object,
            eventStore,
            userStorage.Object,
            companyStorage.Object,
            orchestrator.Object

        )
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext(),
            }
        };
    }
    [Fact]
    public void LoginCallbackDecodeOidcResponse()
    {

        //var mockClient = MockHttpClientFactory.SetupHttpClientFromFile("datasync_meteringpoints.json");

        var signaturGruppen = new SignaturGruppen(new Mock<ILogger<SignaturGruppen>>().Object, authOptionsMock.Object, new HttpClient(), cryptographyFactory.Object.StateCryptography(), jwkService.Object);

        var signaturGruppenResponse = "{ \"id_token\":\"eyJhbGciOiJSUzI1NiIsImtpZCI6IjA0ODA1OEJCNTlGNEQzMDA3MDQ1ODk2RkQ0ODhDRTgxRjRFQjQ5MjMiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL3BwLm5ldHNlaWRicm9rZXIuZGsvb3AiLCJuYmYiOjE2NjE4NDIzNDcsImlhdCI6MTY2MTg0MjM0NywiZXhwIjoxNjYxODQyNjQ3LCJhdWQiOiIwYTc3NWE4Ny04NzhjLTRiODMtYWJlMy1lZTI5YzcyMGMzZTciLCJhbXIiOlsiY29kZV9hcHAiXSwiYXRfaGFzaCI6ImR2TmY5blpVQVotY2I2UjdhZUswU1EiLCJzdWIiOiI5MjM5OTQxMS0xYjk4LTRhMDEtOTIyMS0wNWJhZWJhZDI2NTkiLCJhdXRoX3RpbWUiOjE2NjE4NDIzMDYsImlkcCI6Im1pdGlkIiwiYWNyIjoiaHR0cHM6Ly9kYXRhLmdvdi5kay9jb25jZXB0L2NvcmUvbnNpcy9TdWJzdGFudGlhbCIsIm5lYl9zaWQiOiI5NDZmYjRlZi02ZDlhLTQ0ZjItYjE5Yi00NjRkMTY0MTZjYjciLCJsb2EiOiJodHRwczovL2RhdGEuZ292LmRrL2NvbmNlcHQvY29yZS9uc2lzL1N1YnN0YW50aWFsIiwiYWFsIjoiaHR0cHM6Ly9kYXRhLmdvdi5kay9jb25jZXB0L2NvcmUvbnNpcy9TdWJzdGFudGlhbCIsImlhbCI6Imh0dHBzOi8vZGF0YS5nb3YuZGsvY29uY2VwdC9jb3JlL25zaXMvU3Vic3RhbnRpYWwiLCJpZGVudGl0eV90eXBlIjoicHJpdmF0ZSIsInRyYW5zYWN0aW9uX2lkIjoiYjYyZGZmYzgtYThmMi00MWExLTllNTItOWRiNzFhYTZjMWYwIiwiaWRwX3RyYW5zYWN0aW9uX2lkIjoiODFiOTM3M2EtNGU3YS00MTIxLWFkMTctN2IzNDgxYjFmNjZjIiwic2Vzc2lvbl9leHBpcnkiOiIxNjYxODU4NTAzIn0.qptpC_y946lkOqABNVW-pRUOKTu1rx3iUkxrKhtydG2bVpshBAgmq2gpwZ5KtpJXfpdVbAFaw2JdbSwMG6dnU14xORJdUqnYkzSOaLuALJykf2CK3wzxNFz4LJ_pFIvrh52q0YbUYC5JBIvExU6ugffHhunet1rd8UcLjDjveGAsbFLi8T5IXWBzMDtdnCUEqELa4GzQBQsKKcnmHy8MbpEPD-L9K_HlljN_rAYUbZkIevZCgLkavqt81n2RZtih75qEEmofvAA6bNaYgkd_XlNiTWdYG53zu4Nyc5EUSJqI1eS-P_8TbnNIrFld3L3QK8Tv1VVNVbAbwoeqiyvVNw\", \"access_token\": \"eyJhbGciOiJSUzI1NiIsImtpZCI6IjA0ODA1OEJCNTlGNEQzMDA3MDQ1ODk2RkQ0ODhDRTgxRjRFQjQ5MjMiLCJ0eXAiOiJhdCtqd3QifQ.eyJpc3MiOiJodHRwczovL3BwLm5ldHNlaWRicm9rZXIuZGsvb3AiLCJuYmYiOjE2NjE4NDIzNDcsImlhdCI6MTY2MTg0MjM0NywiZXhwIjoxNjYxODQ1OTQ3LCJzY29wZSI6WyJvcGVuaWQiLCJtaXRpZCJdLCJhbXIiOlsiY29kZV9hcHAiXSwiY2xpZW50X2lkIjoiMGE3NzVhODctODc4Yy00YjgzLWFiZTMtZWUyOWM3MjBjM2U3Iiwic3ViIjoiOTIzOTk0MTEtMWI5OC00YTAxLTkyMjEtMDViYWViYWQyNjU5IiwiYXV0aF90aW1lIjoxNjYxODQyMzA2LCJpZHAiOiJtaXRpZCIsImFjciI6Imh0dHBzOi8vZGF0YS5nb3YuZGsvY29uY2VwdC9jb3JlL25zaXMvU3Vic3RhbnRpYWwiLCJuZWJfc2lkIjoiOTQ2ZmI0ZWYtNmQ5YS00NGYyLWIxOWItNDY0ZDE2NDE2Y2I3IiwiaWRwX3RyYW5zYWN0aW9uX2lkIjoiODFiOTM3M2EtNGU3YS00MTIxLWFkMTctN2IzNDgxYjFmNjZjIiwidHJhbnNhY3Rpb25faWQiOiJiNjJkZmZjOC1hOGYyLTQxYTEtOWU1Mi05ZGI3MWFhNmMxZjAiLCJqdGkiOiJENjFBN0ZDMkEwMUVENUU0QzUxQ0I0NkQ1NTA1NTY5OSJ9.aPLRfFsKUvHpemEGuMLvrogk36finmA_5_O1VnzkJcRknAa7LwAvBRK00c59iOqFWh_oHPBT03JmHLwEFzJSp6lx7QQAz8S8cbZMj - o0lA - UDJqvpkwW4WCdK07_Frh9yVbzuXaAPPLjKdGiLfb44irXJfvSQ6zET35XUSQdQLDLo6X6ihO2qThQ3jSld7i1ndAiWLllDkgOZbTCBfpB4fHvW2cy4wrrYIq5MsvgGkqCfFkzhJWqSdijsNhDK_woeGKtOE1NaNtJQg-XEOsfx7fxGWxHZEs_phbtWPqq2HLyMimyyikhMQab4iEjTz3K_A0eh3j5tqk01ipyZrfOGA\", \"expires_in\": \"3600\", \"token_type\": \"Bearer\", \"scope\": \"openid mitid\"}";

        var token = JsonDocument.Parse(signaturGruppenResponse).RootElement;

        // var deserializedIdToken = signaturGruppen.DecodeOidcResponse(token);



        //Assert.Equal(JsonSerializer.Serialize(expectedDeserializedIdToken), JsonSerializer.Serialize(deserializedIdToken));
    }


    [Fact]
    public void LoginCallbackDeserializeToken()
    {
        var jwtEncoded = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjA0ODA1OEJCNTlGNEQzMDA3MDQ1ODk2RkQ0ODhDRTgxRjRFQjQ5MjMiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL3BwLm5ldHNlaWRicm9rZXIuZGsvb3AiLCJuYmYiOjE2NjE4NDIzNDcsImlhdCI6MTY2MTg0MjM0NywiZXhwIjoxNjYxODQyNjQ3LCJhdWQiOiIwYTc3NWE4Ny04NzhjLTRiODMtYWJlMy1lZTI5YzcyMGMzZTciLCJhbXIiOlsiY29kZV9hcHAiXSwiYXRfaGFzaCI6ImR2TmY5blpVQVotY2I2UjdhZUswU1EiLCJzdWIiOiI5MjM5OTQxMS0xYjk4LTRhMDEtOTIyMS0wNWJhZWJhZDI2NTkiLCJhdXRoX3RpbWUiOjE2NjE4NDIzMDYsImlkcCI6Im1pdGlkIiwiYWNyIjoiaHR0cHM6Ly9kYXRhLmdvdi5kay9jb25jZXB0L2NvcmUvbnNpcy9TdWJzdGFudGlhbCIsIm5lYl9zaWQiOiI5NDZmYjRlZi02ZDlhLTQ0ZjItYjE5Yi00NjRkMTY0MTZjYjciLCJsb2EiOiJodHRwczovL2RhdGEuZ292LmRrL2NvbmNlcHQvY29yZS9uc2lzL1N1YnN0YW50aWFsIiwiYWFsIjoiaHR0cHM6Ly9kYXRhLmdvdi5kay9jb25jZXB0L2NvcmUvbnNpcy9TdWJzdGFudGlhbCIsImlhbCI6Imh0dHBzOi8vZGF0YS5nb3YuZGsvY29uY2VwdC9jb3JlL25zaXMvU3Vic3RhbnRpYWwiLCJpZGVudGl0eV90eXBlIjoicHJpdmF0ZSIsInRyYW5zYWN0aW9uX2lkIjoiYjYyZGZmYzgtYThmMi00MWExLTllNTItOWRiNzFhYTZjMWYwIiwiaWRwX3RyYW5zYWN0aW9uX2lkIjoiODFiOTM3M2EtNGU3YS00MTIxLWFkMTctN2IzNDgxYjFmNjZjIiwic2Vzc2lvbl9leHBpcnkiOiIxNjYxODU4NTAzIn0.qptpC_y946lkOqABNVW-pRUOKTu1rx3iUkxrKhtydG2bVpshBAgmq2gpwZ5KtpJXfpdVbAFaw2JdbSwMG6dnU14xORJdUqnYkzSOaLuALJykf2CK3wzxNFz4LJ_pFIvrh52q0YbUYC5JBIvExU6ugffHhunet1rd8UcLjDjveGAsbFLi8T5IXWBzMDtdnCUEqELa4GzQBQsKKcnmHy8MbpEPD-L9K_HlljN_rAYUbZkIevZCgLkavqt81n2RZtih75qEEmofvAA6bNaYgkd_XlNiTWdYG53zu4Nyc5EUSJqI1eS-P_8TbnNIrFld3L3QK8Tv1VVNVbAbwoeqiyvVNw";

        var deserializedIdToken = loginController.DeserializeToken<IdTokenInfo>(jwtEncoded);

        var expectedDeserializedIdToken = new IdTokenInfo
        {
            Iss = "https://pp.netseidbroker.dk/op",
            Nbf = 1661842347,
            Iat = 1661842347,
            Exp = 1661842647,
            Aud = "0a775a87-878c-4b83-abe3-ee29c720c3e7",
            Amr = new List<string> { "code_app" },
            AtHash = "dvNf9nZUAZ-cb6R7aeK0SQ",
            Sub = "92399411-1b98-4a01-9221-05baebad2659",
            AuthTime = 1661842306,
            Idp = "mitid",
            Acr = "https://data.gov.dk/concept/core/nsis/Substantial",
            NebSid = "946fb4ef-6d9a-44f2-b19b-464d16416cb7",
            Loa = "https://data.gov.dk/concept/core/nsis/Substantial",
            Aal = "https://data.gov.dk/concept/core/nsis/Substantial",
            Ial = "https://data.gov.dk/concept/core/nsis/Substantial",
            IdentityType = "private",
            TransactionId = "b62dffc8-a8f2-41a1-9e52-9db71aa6c1f0",
            IdpTransactionId = "81b9373a-4e7a-4121-ad17-7b3481b1f66c",
            SessionExpiry = "1661858503"
        };

        Assert.Equal(JsonSerializer.Serialize(expectedDeserializedIdToken), JsonSerializer.Serialize(deserializedIdToken));
    }
}
