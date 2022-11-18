using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Client.Dto;
using CertificateEvents.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

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
            DateTimeOffset.FromUnixTimeSeconds(period.DateFrom).ToString("MM/dd/yy H:mm:ss"),
            DateTimeOffset.FromUnixTimeSeconds(period.DateTo).ToString("MM/dd/yy H:mm:ss")
        );

        var url = $"measurements?gsrn={GSRN}&dateFrom={period.DateFrom}&dateTo={period.DateTo}";
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(meteringPointOwner));

        var response = await httpClient.GetAsync(url, cancellationToken);

        return (await response.EnsureSuccessStatusCode().Content
            .ReadFromJsonAsync<List<DataSyncDto>>(cancellationToken: cancellationToken))!;
    }

    private static string GenerateToken(string meteringPointOwner)
    {
        var claims = new Claim[]
        {
            new(JwtRegisteredClaimNames.UniqueName, "username"),
            new(JwtRegisteredClaimNames.NameId, Guid.NewGuid().ToString()),
            new("subject", meteringPointOwner),
            new("actor", "actor"),
            new("scope", "scope"),
        };

        SecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test test test test test"));
        var token = new JwtSecurityToken(
            issuer: "energinet",
            audience: "energinet",
            claims: claims,
            expires: DateTime.Now.AddMinutes(3),
            signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(jwt));
    }
}
