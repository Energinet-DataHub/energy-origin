using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Client.Dto;
using DataContext.ValueObjects;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.Extensions.Logging;

namespace API.DataSyncSyncer.Client;

public class DataSyncClient : IDataSyncClient
{
    private readonly HttpClient httpClient;
    private readonly ILogger<DataSyncClient> logger;

    public DataSyncClient(HttpClient httpClient, ILogger<DataSyncClient> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<List<DataSyncDto>> RequestAsync(string GSRN, Period period, string meteringPointOwner,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Fetching data in period from {from} to: {to}",
            DateTimeOffset.FromUnixTimeSeconds(period.DateFrom).ToString("o"),
            DateTimeOffset.FromUnixTimeSeconds(period.DateTo).ToString("o")
        );

        var url = $"measurements?gsrn={GSRN}&dateFrom={period.DateFrom}&dateTo={period.DateTo}";
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(meteringPointOwner));

        var response = await httpClient.GetAsync(url, cancellationToken);

        response.EnsureSuccessStatusCode();

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
        };

        return (await response.Content
            .ReadFromJsonAsync<List<DataSyncDto>>(jsonSerializerOptions, cancellationToken: cancellationToken))!;
    }

    private static string GenerateToken(string meteringPointOwner)
    {

        var claims = new Dictionary<string, object>()
        {
            { UserClaimName.Scope, "" },
            { UserClaimName.ActorLegacy, "" },
            { UserClaimName.Actor, meteringPointOwner },
            { UserClaimName.Tin, "" },
            { UserClaimName.OrganizationName, "" },
            { JwtRegisteredClaimNames.Name, "" },
            { UserClaimName.ProviderType, ProviderType.MitIdProfessional.ToString()},
            { UserClaimName.AllowCprLookup, "false"},
            { UserClaimName.AccessToken, ""},
            { UserClaimName.IdentityToken, ""},
            { UserClaimName.ProviderKeys, ""},
            { UserClaimName.OrganizationId, meteringPointOwner},
            { UserClaimName.MatchedRoles, ""},
            { UserClaimName.Roles, ""},
            { UserClaimName.AssignedRoles, ""}
        };

        var signedJwtToken = new TokenSigner(RsaKeyGenerator.GenerateTestKey()).Sign(
            meteringPointOwner,
            "name",
            "Us",
            "Users",
            null,
            60,
            claims
        );

        return signedJwtToken;
    }
}
