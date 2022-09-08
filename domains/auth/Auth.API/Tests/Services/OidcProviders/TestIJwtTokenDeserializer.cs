using System.Collections.Generic;
using System.Text.Json;
using API.Services;
using API.Services.OidcProviders.Models.SignaturGruppen;
using Xunit;

namespace Tests.Services.OidcProviders;

public class TestIJwtTokenDeserializer
{
    [Fact]
    public void LoginCallback_DeserializeUserIdToken_Succes()
    {
        var jwtEncoded = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjA0ODA1OEJCNTlGNEQzMDA3MDQ1ODk2RkQ0ODhDRTgxRjRFQjQ5MjMiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL3BwLm5ldHNlaWRicm9rZXIuZGsvb3AiLCJuYmYiOjE2NjE4NDIzNDcsImlhdCI6MTY2MTg0MjM0NywiZXhwIjoxNjYxODQyNjQ3LCJhdWQiOiIwYTc3NWE4Ny04NzhjLTRiODMtYWJlMy1lZTI5YzcyMGMzZTciLCJhbXIiOlsiY29kZV9hcHAiXSwiYXRfaGFzaCI6ImR2TmY5blpVQVotY2I2UjdhZUswU1EiLCJzdWIiOiI5MjM5OTQxMS0xYjk4LTRhMDEtOTIyMS0wNWJhZWJhZDI2NTkiLCJhdXRoX3RpbWUiOjE2NjE4NDIzMDYsImlkcCI6Im1pdGlkIiwiYWNyIjoiaHR0cHM6Ly9kYXRhLmdvdi5kay9jb25jZXB0L2NvcmUvbnNpcy9TdWJzdGFudGlhbCIsIm5lYl9zaWQiOiI5NDZmYjRlZi02ZDlhLTQ0ZjItYjE5Yi00NjRkMTY0MTZjYjciLCJsb2EiOiJodHRwczovL2RhdGEuZ292LmRrL2NvbmNlcHQvY29yZS9uc2lzL1N1YnN0YW50aWFsIiwiYWFsIjoiaHR0cHM6Ly9kYXRhLmdvdi5kay9jb25jZXB0L2NvcmUvbnNpcy9TdWJzdGFudGlhbCIsImlhbCI6Imh0dHBzOi8vZGF0YS5nb3YuZGsvY29uY2VwdC9jb3JlL25zaXMvU3Vic3RhbnRpYWwiLCJpZGVudGl0eV90eXBlIjoicHJpdmF0ZSIsInRyYW5zYWN0aW9uX2lkIjoiYjYyZGZmYzgtYThmMi00MWExLTllNTItOWRiNzFhYTZjMWYwIiwiaWRwX3RyYW5zYWN0aW9uX2lkIjoiODFiOTM3M2EtNGU3YS00MTIxLWFkMTctN2IzNDgxYjFmNjZjIiwic2Vzc2lvbl9leHBpcnkiOiIxNjYxODU4NTAzIn0.qptpC_y946lkOqABNVW-pRUOKTu1rx3iUkxrKhtydG2bVpshBAgmq2gpwZ5KtpJXfpdVbAFaw2JdbSwMG6dnU14xORJdUqnYkzSOaLuALJykf2CK3wzxNFz4LJ_pFIvrh52q0YbUYC5JBIvExU6ugffHhunet1rd8UcLjDjveGAsbFLi8T5IXWBzMDtdnCUEqELa4GzQBQsKKcnmHy8MbpEPD-L9K_HlljN_rAYUbZkIevZCgLkavqt81n2RZtih75qEEmofvAA6bNaYgkd_XlNiTWdYG53zu4Nyc5EUSJqI1eS-P_8TbnNIrFld3L3QK8Tv1VVNVbAbwoeqiyvVNw";

        var deserializedIdToken = new JwtDeserializer().DeserializeJwt<IdTokenInfo>(jwtEncoded);
        var expectedDeserializedIdToken = new IdTokenInfo
        (
            Iss: "https://pp.netseidbroker.dk/op",
            Nbf: 1661842347,
            Iat: 1661842347,
            Exp: 1661842647,
            Aud: "0a775a87-878c-4b83-abe3-ee29c720c3e7",
            Amr: new List<string> { "code_app" },
            AtHash: "dvNf9nZUAZ-cb6R7aeK0SQ",
            Sub: "92399411-1b98-4a01-9221-05baebad2659",
            AuthTime: 1661842306,
            Idp: "mitid",
            Acr: "https://data.gov.dk/concept/core/nsis/Substantial",
            NebSid: "946fb4ef-6d9a-44f2-b19b-464d16416cb7",
            Loa: "https://data.gov.dk/concept/core/nsis/Substantial",
            Aal: "https://data.gov.dk/concept/core/nsis/Substantial",
            Ial: "https://data.gov.dk/concept/core/nsis/Substantial",
            IdentityType: "private",
            TransactionId: "b62dffc8-a8f2-41a1-9e52-9db71aa6c1f0",
            IdpTransactionId: "81b9373a-4e7a-4121-ad17-7b3481b1f66c",
            SessionExpiry: "1661858503"
        );

        Assert.Equal(JsonSerializer.Serialize(expectedDeserializedIdToken), JsonSerializer.Serialize(deserializedIdToken));
    }

    [Fact]
    public void LoginCallback_DeserializeUserInfoToken_Succes()
    {
        var jwtEncoded = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJodHRwczovL3BwLm5ldHNlaWRicm9rZXIuZGsvb3AiLCJuYmYiOjE2NDMyOTA4OTUsImlhdCI6MTY0MzI5MDg5NSwiZXhwIjoxNjQzMjkxMTk1LCJhbXIiOlsibmVtaWQub3RwIl0sImlkcCI6Im5lbWlkIiwibmVtaWQuc3NuIjoiQ1ZSOjM5MzE1MDQxLVJJRDozNTYxMzMzMCIsIm5lbWlkLmNvbW1vbl9uYW1lIjoiVEVTVCAtIFRlc3QgVGVzdGVzZW4iLCJuZW1pZC5kbiI6IkNOPVRFU1QgLSBUZXN0IFRlc3Rlc2VuK1NFUklBTE5VTUJFUj1DVlI6MzkzMTUwNDEtUklEOjM1NjEzMzMwLE89RW5lcmdpbmV0IERhdGFIdWIgQS9TIC8vIENWUjozOTMxNTA0MSxDPURLIiwibmVtaWQucmlkIjoiMzU2MTMzMzAiLCJuZW1pZC5jb21wYW55X25hbWUiOiJFbmVyZ2luZXQgRGF0YUh1YiBBL1MgIiwibmVtaWQuY3ZyIjoiMzkzMTUwNDEiLCJpZGVudGl0eV90eXBlIjoicHJvZmVzc2lvbmFsIiwiYXV0aF90aW1lIjoiMTY0MzI5MDg5NSIsInN1YiI6IjNlNGY5M2NkLTY0NDgtNDgwZC04ODY4LThlZmY5MTEyNGZjNiIsInRyYW5zYWN0aW9uX2lkIjoiMjM2NTMxNmEtMWYxYy00OGU2LTk4ZTctYWNmNmQ3ZWMzOGMxIiwiYXVkIjoiMGE3NzVhODctODc4Yy00YjgzLWFiZTMtZWUyOWM3MjBjM2U3In0.RR8rtwpfd_ixOLkcQ-c4vXp4uKhMdS756znoMUS87j0";

        var deserializedIdToken = new JwtDeserializer().DeserializeJwt<UserInfoToken>(jwtEncoded);

        var expectedDeserializedUserInfoToken = new UserInfoToken
        (
            Iss: "https://pp.netseidbroker.dk/op",
            Nbf: 1643290895,
            Iat: 1643290895,
            Exp: 1643291195,
            Amr: new List<string> { "nemid.otp" },
            Idp: "nemid",
            NemidSsn: "CVR:39315041-RID:35613330",
            NemidCommonName: "TEST - Test Testesen",
            NemidDn: "CN=TEST - Test Testesen+SERIALNUMBER=CVR:39315041-RID:35613330,O=Energinet DataHub A/S // CVR:39315041,C=DK",
            NemidRid: "35613330",
            NemidCompanyName: "Energinet DataHub A/S ",
            NemidCvr: "39315041",
            IdentityType: "professional",
            AuthTime: "1643290895",
            Sub: "3e4f93cd-6448-480d-8868-8eff91124fc6",
            TransactionId: "2365316a-1f1c-48e6-98e7-acf6d7ec38c1",
            Aud: "0a775a87-878c-4b83-abe3-ee29c720c3e7"
        );

        Assert.Equal(JsonSerializer.Serialize(expectedDeserializedUserInfoToken), JsonSerializer.Serialize(deserializedIdToken));
    }
}
