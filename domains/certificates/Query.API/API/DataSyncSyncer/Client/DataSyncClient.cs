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
            DateTimeOffset.FromUnixTimeSeconds(period.DateFrom).ToString("o"),
            DateTimeOffset.FromUnixTimeSeconds(period.DateTo).ToString("o")
        );

        var url = $"measurements?gsrn={GSRN}&dateFrom={period.DateFrom}&dateTo={period.DateTo}";
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(meteringPointOwner));

        var response = await httpClient.GetAsync(url, cancellationToken);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<List<DataSyncDto>>(cancellationToken: cancellationToken))!;
    }

    private static string GenerateToken(string meteringPointOwner)
    {
        var expires = DateTime.Now.AddMinutes(3);
        var claims = new Claim[]
        {
            new("subject", meteringPointOwner),
            new("actor", meteringPointOwner),
            new("issued", DateTimeOffset.Now.ToString()),
            new("expires", ((DateTimeOffset)expires).ToString()),
            new("scope", "meteringpoints.read"),
            new("scope", "measurements.read"),
        };


        SecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test test test test test"));
        var token = new JwtSecurityToken(
            issuer: "energinet",
            audience: "energinet",
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(jwt));
    }
}
