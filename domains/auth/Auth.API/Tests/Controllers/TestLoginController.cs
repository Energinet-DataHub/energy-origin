using System.Collections.Generic;
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
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace API.Controllers;

public class TestLoginController
{
    private readonly Mock<IOidcService> mockSignaturGruppen = new();
    private readonly Mock<IOptions<AuthOptions>> authOptionsMock = new();
    private readonly Mock<ITokenStorage> tokenStorage = new();
    private readonly Mock<ICryptographyFactory> stateCryptography = new();
    private readonly Mock<ICryptographyFactory> idTokencryptography = new();
    private readonly Mock<IValidator<OidcCallbackParams>> validator = new();
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
            stateCryptography.Object,
            idTokencryptography.Object,
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

    //[Fact]
    //public void LoginCallback_DecodeOidcResponse()
    //{

    //    var oidcTokenResponse = new OidcTokenResponse
    //    {
    //        IdToken = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjA0ODA1OEJCNTlGNEQzMDA3MDQ1ODk2RkQ0ODhDRTgxRjRFQjQ5MjMiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL3BwLm5ldHNlaWRicm9rZXIuZGsvb3AiLCJuYmYiOjE2NjIzNjIxMTYsImlhdCI6MTY2MjM2MjExNiwiZXhwIjoxNjYyMzYyNDE2LCJhdWQiOiIwYTc3NWE4Ny04NzhjLTRiODMtYWJlMy1lZTI5YzcyMGMzZTciLCJhbXIiOlsiY29kZV9hcHAiXSwiYXRfaGFzaCI6Ii1SbklJOGFXQWxDXzJKRFlQcEp3SlEiLCJzdWIiOiIzZTRmOTNjZC02NDQ4LTQ4MGQtODg2OC04ZWZmOTExMjRmYzYiLCJhdXRoX3RpbWUiOjE2NjIzNjIwMTQsImlkcCI6Im1pdGlkIiwiYWNyIjoiaHR0cHM6Ly9kYXRhLmdvdi5kay9jb25jZXB0L2NvcmUvbnNpcy9TdWJzdGFudGlhbCIsIm5lYl9zaWQiOiIzYjFmMDFjNS05ZGRkLTQ3MzItODljMy0wZWYxNDU5ODAxN2MiLCJsb2EiOiJodHRwczovL2RhdGEuZ292LmRrL2NvbmNlcHQvY29yZS9uc2lzL1N1YnN0YW50aWFsIiwiYWFsIjoiaHR0cHM6Ly9kYXRhLmdvdi5kay9jb25jZXB0L2NvcmUvbnNpcy9TdWJzdGFudGlhbCIsImlhbCI6Imh0dHBzOi8vZGF0YS5nb3YuZGsvY29uY2VwdC9jb3JlL25zaXMvU3Vic3RhbnRpYWwiLCJpZGVudGl0eV90eXBlIjoicHJpdmF0ZSIsInRyYW5zYWN0aW9uX2lkIjoiMjM2NTMxNmEtMWYxYy00OGU2LTk4ZTctYWNmNmQ3ZWMzOGMxIiwiaWRwX3RyYW5zYWN0aW9uX2lkIjoiNDRhNDQyZWQtZDE0Mi00ZGQyLTkxNmYtNjY4MTQxMjE3ZjBmIiwic2Vzc2lvbl9leHBpcnkiOiIxNjYyMzc4MjExIn0.hE1PW3zMIxmokUcdWcYG6w-imu38Jk3OphDYVhbYKFqCkjTuKnrkinSZyWFopXnZsffQIxYRe3nh5391mxLpgfG0K_Kh-Ninsi2f3h4Vk5rligdfq2j_5aPJg3ku8bDH1X4Hy934E_zobceKSkp8P35ob4MJV6kwu3t6CIs_K-lAOT_mFuOd3HQOplCMS3gE-c2GSPeM3Buggz7WaZG2hXXT5n2w3iVwIF-DZmpkdAyByHaFD9cSPvICo0oyEdKhHz1XUtNhSDmz5tyiuXQER7XdSLVLqBrB-sJsWouKCEwyYUHIQYosL7Fcz8WQ0lfD0x69Rgt-gsVKjJoteeGhjw",
    //        AccessToken = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjA0ODA1OEJCNTlGNEQzMDA3MDQ1ODk2RkQ0ODhDRTgxRjRFQjQ5MjMiLCJ0eXAiOiJhdCtqd3QifQ.eyJpc3MiOiJodHRwczovL3BwLm5ldHNlaWRicm9rZXIuZGsvb3AiLCJuYmYiOjE2NjIzNjIxMTYsImlhdCI6MTY2MjM2MjExNiwiZXhwIjoxNjYyMzY1NzE2LCJzY29wZSI6WyJvcGVuaWQiLCJtaXRpZCIsIm5lbWlkIiwidXNlcmluZm9fdG9rZW4iXSwiYW1yIjpbImNvZGVfYXBwIl0sImNsaWVudF9pZCI6IjBhNzc1YTg3LTg3OGMtNGI4My1hYmUzLWVlMjljNzIwYzNlNyIsInN1YiI6IjNlNGY5M2NkLTY0NDgtNDgwZC04ODY4LThlZmY5MTEyNGZjNiIsImF1dGhfdGltZSI6MTY2MjM2MjAxNCwiaWRwIjoibWl0aWQiLCJhY3IiOiJodHRwczovL2RhdGEuZ292LmRrL2NvbmNlcHQvY29yZS9uc2lzL1N1YnN0YW50aWFsIiwibmViX3NpZCI6IjNiMWYwMWM1LTlkZGQtNDczMi04OWMzLTBlZjE0NTk4MDE3YyIsImlkcF90cmFuc2FjdGlvbl9pZCI6IjQ0YTQ0MmVkLWQxNDItNGRkMi05MTZmLTY2ODE0MTIxN2YwZiIsInRyYW5zYWN0aW9uX2lkIjoiMjM2NTMxNmEtMWYxYy00OGU2LTk4ZTctYWNmNmQ3ZWMzOGMxIiwianRpIjoiMkZDRjQ0N0U1MzgxN0Y1M0Y4M0Y4REYyNEZDODNGMTUifQ.GKIly60ILu8-g_wnAGA1m5b4wxdVBymtr_LnGTPFE6v6PyX5g1KncFLcHUSQfiiysalNoyqqCjaWaReTnHj5TurTRB49YMPK9rNDiUHtLhENETaYe_R3xXn-wh1lx81NQxwxG2Fob4wRw7fG208bEMFjoFMGfFWFir_JP1b_9gFF-bUsFeItjhDAowj4wBuOYZXqPzIAXZ6M_iRJu2vBOU4r155ImmY1xUplQjBtctMvViiMHQKuESd6oy0_nAx3Z2iFT9Qq2qi9ZdxKFFIt7cE0QawWDoYBb4sZCW7qkMWMnnnro1dsH6N1DCo6ib666oF6pZoIP1OZc628pCWOMQ",
    //        ExpiresIn = 3600,
    //        TokenType = "Bearer",
    //        Scope = "openid mitid nemid userinfo_token",
    //        UserinfoToken = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjA0ODA1OEJCNTlGNEQzMDA3MDQ1ODk2RkQ0ODhDRTgxRjRFQjQ5MjMiLCJ0eXAiOiJhdCtqd3QifQ.eyJpc3MiOiJodHRwczovL3BwLm5ldHNlaWRicm9rZXIuZGsvb3AiLCJuYmYiOjE2NjIzNjIxMTYsImlhdCI6MTY2MjM2MjExNiwiZXhwIjoxNjYyMzYyNDE2LCJhbXIiOlsiY29kZV9hcHAiXSwibWl0aWQudXVpZCI6IjM0YjFhZDlhLWRiMGItNDFlYy1hYTlkLTI5MDkzMDgyNTNhOSIsIm1pdGlkLmFnZSI6IjM4IiwibWl0aWQuZGF0ZV9vZl9iaXJ0aCI6IjE5ODQtMDctMjgiLCJtaXRpZC5pZGVudGl0eV9uYW1lIjoiQXlhIEhhbnNlbiIsIm1pdGlkLnRyYW5zYWN0aW9uX2lkIjoiNDRhNDQyZWQtZDE0Mi00ZGQyLTkxNmYtNjY4MTQxMjE3ZjBmIiwibG9hIjoiaHR0cHM6Ly9kYXRhLmdvdi5kay9jb25jZXB0L2NvcmUvbnNpcy9TdWJzdGFudGlhbCIsImFjciI6Imh0dHBzOi8vZGF0YS5nb3YuZGsvY29uY2VwdC9jb3JlL25zaXMvU3Vic3RhbnRpYWwiLCJpYWwiOiJodHRwczovL2RhdGEuZ292LmRrL2NvbmNlcHQvY29yZS9uc2lzL1N1YnN0YW50aWFsIiwiYWFsIjoiaHR0cHM6Ly9kYXRhLmdvdi5kay9jb25jZXB0L2NvcmUvbnNpcy9TdWJzdGFudGlhbCIsImlkZW50aXR5X3R5cGUiOiJwcml2YXRlIiwiaWRwIjoibWl0aWQiLCJhdXRoX3RpbWUiOiIxNjYyMzYyMDE0Iiwic3ViIjoiM2U0ZjkzY2QtNjQ0OC00ODBkLTg4NjgtOGVmZjkxMTI0ZmM2IiwidHJhbnNhY3Rpb25faWQiOiIyMzY1MzE2YS0xZjFjLTQ4ZTYtOThlNy1hY2Y2ZDdlYzM4YzEiLCJhdWQiOiIwYTc3NWE4Ny04NzhjLTRiODMtYWJlMy1lZTI5YzcyMGMzZTcifQ.MBISWh11OxlsKDaaug8jMQotEB-aJn2jAFEmCOPc5zJUVcTBWZJ1FFAx2g7r1p_8-WK5Us69yeBk5yu1X-t2l7aqvdV0a6oKntAdARquV4bD35ezoXaPUpWe-crMuzA1mfQa9mzAc8FB837k5p1WOplAA-51GFCTylMw-D0FSxxwbtvMUIDpFaTZGZLIqZ-8N3QPyw0Ib7qO_vXvWd-z3UuCBMYPaV0NMbWoHdXYTNORD18i1kbOQFmgRDnXH6din2qfdv6QEBYTkxlorq24MjjEegwNQuJAzpXJy5LbbEz2wcDu06JNP-LnXYGVU_H_jjo_Lu_armuK55u0TcpFxg"
    //    };

    //    var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? throw new Exception("Invalid directory");
    //    var path = Path.Combine(directory, "../../../Resources/", "jwks.json");
    //    var json = File.ReadAllText(path);

    //    // var jwkSet = JwkSet.FromJson(json);
    //    //var jwkSet = JwkSet.FromJson(json, new JsonMapper());
    //    //var jwk = jwkSet.Keys.FirstOrDefault();


    //    var jwkService = new Mock<IJwkService>();
    //    jwkService.Setup(x => x.GetJwkAsync()).Returns(Task.FromResult(json));

    //    var signaturGruppen = new SignaturGruppen(new Mock<ILogger<SignaturGruppen>>().Object, authOptionsMock.Object, new HttpClient(), cryptographyFactory.Object.StateCryptography(), jwkService.Object);


    //    var deserializedIdToken = signaturGruppen.DecodeOidcResponse(oidcTokenResponse);



    //    //Assert.Equal(JsonSerializer.Serialize(expectedDeserializedIdToken), JsonSerializer.Serialize(deserializedIdToken));
    //}




    [Fact]
    public void LoginCallback_DeserializeUserIdToken_Succes()
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

    [Fact]
    public void LoginCallback_DeserializeUserInfoToken_Succes()
    {
        var jwtEncoded = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJodHRwczovL3BwLm5ldHNlaWRicm9rZXIuZGsvb3AiLCJuYmYiOjE2NDMyOTA4OTUsImlhdCI6MTY0MzI5MDg5NSwiZXhwIjoxNjQzMjkxMTk1LCJhbXIiOlsibmVtaWQub3RwIl0sImlkcCI6Im5lbWlkIiwibmVtaWQuc3NuIjoiQ1ZSOjM5MzE1MDQxLVJJRDozNTYxMzMzMCIsIm5lbWlkLmNvbW1vbl9uYW1lIjoiVEVTVCAtIFRlc3QgVGVzdGVzZW4iLCJuZW1pZC5kbiI6IkNOPVRFU1QgLSBUZXN0IFRlc3Rlc2VuK1NFUklBTE5VTUJFUj1DVlI6MzkzMTUwNDEtUklEOjM1NjEzMzMwLE89RW5lcmdpbmV0IERhdGFIdWIgQS9TIC8vIENWUjozOTMxNTA0MSxDPURLIiwibmVtaWQucmlkIjoiMzU2MTMzMzAiLCJuZW1pZC5jb21wYW55X25hbWUiOiJFbmVyZ2luZXQgRGF0YUh1YiBBL1MgIiwibmVtaWQuY3ZyIjoiMzkzMTUwNDEiLCJpZGVudGl0eV90eXBlIjoicHJvZmVzc2lvbmFsIiwiYXV0aF90aW1lIjoiMTY0MzI5MDg5NSIsInN1YiI6IjNlNGY5M2NkLTY0NDgtNDgwZC04ODY4LThlZmY5MTEyNGZjNiIsInRyYW5zYWN0aW9uX2lkIjoiMjM2NTMxNmEtMWYxYy00OGU2LTk4ZTctYWNmNmQ3ZWMzOGMxIiwiYXVkIjoiMGE3NzVhODctODc4Yy00YjgzLWFiZTMtZWUyOWM3MjBjM2U3In0.RR8rtwpfd_ixOLkcQ-c4vXp4uKhMdS756znoMUS87j0";

        var deserializedIdToken = loginController.DeserializeToken<UserInfoToken>(jwtEncoded);

        var expectedDeserializedUserInfoToken = new UserInfoToken
        {
            Iss = "https://pp.netseidbroker.dk/op",
            Nbf = 1643290895,
            Iat = 1643290895,
            Exp = 1643291195,
            Amr = new List<string> { "nemid.otp" },
            Idp = "nemid",
            NemidSsn = "CVR:39315041-RID:35613330",
            NemidCommonName = "TEST - Test Testesen",
            NemidDn = "CN=TEST - Test Testesen+SERIALNUMBER=CVR:39315041-RID:35613330,O=Energinet DataHub A/S // CVR:39315041,C=DK",
            NemidRid = "35613330",
            NemidCompanyName = "Energinet DataHub A/S ",
            NemidCvr = "39315041",
            IdentityType = "professional",
            AuthTime = "1643290895",
            Sub = "3e4f93cd-6448-480d-8868-8eff91124fc6",
            TransactionId = "2365316a-1f1c-48e6-98e7-acf6d7ec38c1",
            Aud = "0a775a87-878c-4b83-abe3-ee29c720c3e7"
        };

        Assert.Equal(JsonSerializer.Serialize(expectedDeserializedUserInfoToken), JsonSerializer.Serialize(deserializedIdToken));
    }
}


