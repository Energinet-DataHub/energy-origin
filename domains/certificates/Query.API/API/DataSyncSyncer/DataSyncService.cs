using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Dto;
using CertificateEvents.Primitives;
using Marten;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace API.DataSyncSyncer;

public class DataSyncService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<DataSyncService> logger;
    private Dictionary<string, DateTimeOffset>? periodStartTimeDictionary;

    public DataSyncService(HttpClient httpClient, ILogger<DataSyncService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public void SetState(Dictionary<string, DateTimeOffset> state)
    {
        periodStartTimeDictionary = state;
    }

    public async Task<List<DataSyncDto>> FetchMeasurements(string GSRN, string meteringPointOwner,
        CancellationToken cancellationToken)
    {
        var dateFrom = periodStartTimeDictionary[GSRN].ToUnixTimeSeconds();

        var now = DateTimeOffset.UtcNow;
        var midnight = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var result = new List<DataSyncDto>();

        if (dateFrom < midnight)
        {
            try
            {
                result = await RequestDatasync(
                    GSRN,
                    new Period(
                        DateFrom: dateFrom,
                        DateTo: midnight
                    ),
                    meteringPointOwner,
                    cancellationToken
                );
            }
            catch (Exception e)
            {
                logger.LogInformation("An error occured: {error}, no measurements was fetched", e.Message);
            }

        }

        SetNextPeriodStartTime(result, GSRN);
        return result;
    }

    private void SetNextPeriodStartTime(List<DataSyncDto> measurements, string GSRN)
    {
        if (measurements.IsEmpty())
        {
            return;
        }

        var newestMeasurement = measurements.Max(m => m.DateTo);
        periodStartTimeDictionary![GSRN] = DateTimeOffset.FromUnixTimeSeconds(newestMeasurement);
    }

    private async Task<List<DataSyncDto>> RequestDatasync(string gsrn, Period period, string meteringPointOwner,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Fetching data in period from {from} to: {to}", period.DateFrom.ToString("MM/dd/yy H:mm:ss"), period.DateTo.ToString("MM/dd/yy H:mm:ss")
        );

        var url = $"measurements?gsrn={gsrn}&dateFrom={period.DateFrom}&dateTo={period.DateTo}";
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(meteringPointOwner));

        var response = await httpClient.GetAsync(url, cancellationToken);

        return await response.Content.ReadFromJsonAsync<List<DataSyncDto>>(cancellationToken: cancellationToken)
               ?? throw new Exception($"Fetch of measurements failed, base: {httpClient.BaseAddress} url: {url}");
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
