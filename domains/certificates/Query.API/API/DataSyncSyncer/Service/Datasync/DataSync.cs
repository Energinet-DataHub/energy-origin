using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using API.DataSyncSyncer.Service.Datasync.Configurations;
using API.MasterDataService;
using CertificateEvents;
using CertificateEvents.Primitives;
using EnergyOriginDateTimeExtension;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace API.DataSyncSyncer.Service.Datasync;

public class DataSync : IDataSync
{
    private readonly HttpClient httpClient;
    private readonly DatasyncOptions options;
    private readonly ILogger<DataSync> logger;

    public DataSync(HttpClient httpClient, IOptions<DatasyncOptions> options, ILogger<DataSync> logger)
    {
        this.logger = logger;
        this.httpClient = httpClient;
        this.options = options.Value;
    }

    public async Task<EnergyMeasuredIntegrationEvent> GetMeasurement(string gsrn, Period period)
    {
        logger.LogInformation(
            "Fetching data in period from {from} to: {to}", period.DateFrom, period.DateTo
        );

        var url = $"{options.Url}/measurements?gsrn={gsrn}&dateFrom={period.DateFrom}&dateTo={period.DateTo}";

        httpClient.SetBearerToken(GenerateToken());

        var response = await httpClient.GetAsync(url);

        if (response == null || !response.IsSuccessStatusCode)
        {
            throw new Exception($"Fetch of measurements failed, base: {httpClient.BaseAddress} url: {url}");
        }

        return await response.Content.ReadFromJsonAsync<EnergyMeasuredIntegrationEvent>();
    }

    public static string GenerateToken()
    {
        var claims = new Claim[]
        {
            new(JwtRegisteredClaimNames.UniqueName, "username"),
            new(JwtRegisteredClaimNames.NameId, Guid.NewGuid().ToString())
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
