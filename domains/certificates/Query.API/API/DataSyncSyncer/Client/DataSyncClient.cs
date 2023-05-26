using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Client.Dto;
using CertificateValueObjects;
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
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(3);

        var claims = new Claim[]
        {
            new("subject", meteringPointOwner),
            new("actor", meteringPointOwner),
            new("issued", now.ToString("o")),
            new("expires", expires.ToString("o")),
            new("scope", "meteringpoints.read"),
            new("scope", "measurements.read"),
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("TESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTEST");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires.DateTime,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
