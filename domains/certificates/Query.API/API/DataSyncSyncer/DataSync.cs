using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CertificateEvents.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace API.DataSyncSyncer.Service.Datasync;

public class DataSync : IDataSync
{
    private readonly HttpClient httpClient;
    private readonly ILogger<DataSync> logger;

    public DataSync(HttpClient httpClient, ILogger<DataSync> logger)
    {
        this.logger = logger;
        this.httpClient = httpClient;
    }

    public async Task<List<DataSyncDto>> GetMeasurement(string gsrn, Period period, string meteringPointOwner)
    {
        logger.LogInformation(
            "Fetching data in period from {from} to: {to}", period.DateFrom, period.DateTo
        );

        var url = $"measurements?gsrn={gsrn}&dateFrom={period.DateFrom}&dateTo={period.DateTo}";
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(meteringPointOwner));

        var response = await httpClient.GetAsync(url);

        return await response.Content.ReadFromJsonAsync<List<DataSyncDto>>()
               ?? throw new Exception($"Fetch of measurements failed, base: {httpClient.BaseAddress} url: {url}");
    }

    public static string GenerateToken(string meteringPointOwner)
    {
        var claims = new Claim[]
        {
            new(JwtRegisteredClaimNames.UniqueName, "username"),
            new(JwtRegisteredClaimNames.NameId, Guid.NewGuid().ToString()),
            new("subject",  meteringPointOwner),
            new("actor",  "actor"),
            new("scope",  "scope"),
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
