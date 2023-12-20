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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TransferAgreementAutomation.Worker.Metrics;
using TransferAgreementAutomation.Worker.Models;
using TransferAgreementAutomation.Worker.Options;
using TransferAgreementAutomation.Worker.Service;

namespace TransferAgreementAutomation.Worker;

public class TransferAgreementsAutomationWorker : BackgroundService
{
    private readonly ILogger<TransferAgreementsAutomationWorker> logger;
    private readonly ITransferAgreementAutomationMetrics metrics;
    private readonly AutomationCache memoryCache;
    private readonly IServiceProvider serviceProvider;
    private readonly IHttpClientFactory httpClient;
    private readonly TransferApiOptions options;

    private readonly MemoryCacheEntryOptions cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
    };

    public TransferAgreementsAutomationWorker(
        ILogger<TransferAgreementsAutomationWorker> logger,
        ITransferAgreementAutomationMetrics metrics,
        AutomationCache memoryCache,
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClient,
        IOptions<TransferApiOptions> options)
    {
        this.logger = logger;
        this.metrics = metrics;
        this.memoryCache = memoryCache;
        this.serviceProvider = serviceProvider;
        this.httpClient = httpClient;
        this.options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("TransferAgreementsAutomationWorker running at: {time}", DateTimeOffset.Now);
            metrics.ResetCertificatesTransferred();
            metrics.ResetTransferErrors();
            memoryCache.Cache.Set(HealthEntries.Key, HealthEntries.Healthy, cacheOptions);

            using var scope = serviceProvider.CreateScope();
            var projectOriginWalletService = scope.ServiceProvider.GetRequiredService<IProjectOriginWalletService>();

            try
            {
                var transferAgreements = await GetAllTransferAgreements(stoppingToken);
                metrics.SetNumberOfTransferAgreements(transferAgreements.Result.Count);

                foreach (var transferAgreement in transferAgreements.Result)
                {
                    await projectOriginWalletService.TransferCertificates(transferAgreement);
                }
            }
            catch (Exception ex)
            {
                memoryCache.Cache.Set(HealthEntries.Key, HealthEntries.Unhealthy, cacheOptions);
                logger.LogWarning("Something went wrong with the TransferAgreementsAutomationWorker: {exception}", ex);
            }

            await SleepToNearestHour(stoppingToken);
        }
    }

    private async Task<TransferAgreementsDto> GetAllTransferAgreements(CancellationToken stoppingToken)
    {
        var client = httpClient.CreateClient();

        client.BaseAddress = new Uri(options.Url);
        client.DefaultRequestHeaders.Add("EO_API_VERSION", options.Version);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken());

        var response = await client.GetAsync("api/transfer-agreements/all", stoppingToken);

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
        };

        return (await response.Content
            .ReadFromJsonAsync<TransferAgreementsDto>(jsonSerializerOptions, cancellationToken: stoppingToken))!;
    }

    private async Task SleepToNearestHour(CancellationToken cancellationToken)
    {
        var minutesToNextHour = 60 - DateTimeOffset.Now.Minute;
        logger.LogInformation("Sleeping until next full hour {minutesToNextHour}", minutesToNextHour);
        await Task.Delay(TimeSpan.FromMinutes(minutesToNextHour), cancellationToken);
    }

    private static string GenerateToken()
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(3);

        var claims = new Claim[]
        {
            new("issued", now.ToString("o")),
            new("expires", expires.ToString("o")),
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("TESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTEST");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires.DateTime,
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
