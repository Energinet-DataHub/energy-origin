using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.IntegrationTests.Mocks;
using API.MeasurementsSyncer;
using API.Query.API.Controllers;
using Asp.Versioning.ApiExplorer;
using Contracts;
using DataContext;
using DataContext.ValueObjects;
using EnergyOrigin.ActivityLog;
using EnergyOrigin.TokenValidation.b2c;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using FluentAssertions;
using Grpc.Net.Client;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NBitcoin;
using Polly;
using AuthenticationScheme = EnergyOrigin.TokenValidation.b2c.AuthenticationScheme;
using Policy = EnergyOrigin.TokenValidation.b2c.Policy;
using Technology = API.ContractService.Clients.Technology;

namespace API.IntegrationTests.Factories;

public class QueryApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly List<GrpcChannel> disposableChannels = new();
    private HttpClient? client;
    public string ConnectionString { get; set; } = "";
    public string MeasurementsUrl { get; set; } = "http://foo";
    public string WalletUrl { get; set; } = "bar";

    private string OtlpReceiverEndpoint { get; set; } = "http://foo";
    public RabbitMqOptions? RabbitMqOptions { get; set; }
    private byte[] PrivateKey { get; set; } = RsaKeyGenerator.GenerateTestKey();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var privateKeyPem = Encoding.UTF8.GetString(PrivateKey);
        string publicKeyPem;

        using (RSA rsa = RSA.Create())
        {
            rsa.ImportFromPem(privateKeyPem);
            publicKeyPem = rsa.ExportRSAPublicKeyPem();
        }

        var publicKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKeyPem));

        builder.UseSetting("Otlp:ReceiverEndpoint", OtlpReceiverEndpoint);
        builder.UseSetting("ConnectionStrings:Postgres", ConnectionString);
        builder.UseSetting("Measurements:Url", MeasurementsUrl);
        builder.UseSetting("Measurements:GrpcUrl", "http://foo");
        builder.UseSetting("MeasurementsSync:Disabled", "false");
        builder.UseSetting("IssuingContractCleanup:SleepTime", "00:00:03");
        builder.UseSetting("Wallet:Url", WalletUrl);
        builder.UseSetting("RabbitMq:Password", RabbitMqOptions?.Password ?? "");
        builder.UseSetting("RabbitMq:Username", RabbitMqOptions?.Username ?? "");
        builder.UseSetting("RabbitMq:Host", RabbitMqOptions?.Host ?? "localhost");
        builder.UseSetting("RabbitMq:Port", RabbitMqOptions?.Port.ToString() ?? "4242");
        builder.UseSetting("TokenValidation:PublicKey", publicKeyBase64);
        builder.UseSetting("TokenValidation:Issuer", "demo.energioprindelse.dk");
        builder.UseSetting("TokenValidation:Audience", "Users");

        builder.UseSetting("B2C:B2CWellKnownUrl",
            "https://login.microsoftonline.com/d3803538-de83-47f3-bc72-54843a8592f2/v2.0/.well-known/openid-configuration");
        builder.UseSetting("B2C:ClientCredentialsCustomPolicyWellKnownUrl",
            "https://datahubeouenerginet.b2clogin.com/datahubeouenerginet.onmicrosoft.com/v2.0/.well-known/openid-configuration?p=B2C_1A_CLIENTCREDENTIALS");
        builder.UseSetting("B2C:MitIDCustomPolicyWellKnownUrl",
            "https://datahubeouenerginet.b2clogin.com/datahubeouenerginet.onmicrosoft.com/v2.0/.well-known/openid-configuration?p=B2C_1A_MITID");
        builder.UseSetting("B2C:Audience", "f00b9b4d-3c59-4c40-b209-2ef87e509f54");
        builder.UseSetting("B2C:CustomPolicyClientId", "a701d13c-2570-46fa-9aa2-8d81f0d8d60b");

        builder.ConfigureTestServices(services =>
        {
            services.Configure<ActivityLogOptions>(options =>
            {
                options.ServiceName = "certificates";
                options.CleanupActivityLogsOlderThanInDays = 1;
                options.CleanupIntervalInSeconds = 3;
            });

            services.AddOptions<MassTransitHostOptions>().Configure(options =>
            {
                options.StartTimeout = TimeSpan.FromSeconds(30);
                options.StopTimeout = TimeSpan.FromSeconds(5);
                // Ensure masstransit bus is started when we run our health checks
                options.WaitUntilStarted = RabbitMqOptions != null;
            });

            // Remove MeasurementsSyncerWorker
            services.Remove(services.First(s => s.ImplementationType == typeof(MeasurementsSyncerWorker)));

            // var serviceDescriptor = services.FirstOrDefault(s => typeof(IAuthenticationSchemeProvider).IsAssignableFrom(s.ServiceType));
            // if (serviceDescriptor is not null)
            // {
            //     services.Remove(serviceDescriptor);
            // }
            //
            // services.AddSingleton<IAuthenticationSchemeProvider, IntegrationTestAuthenticationSchemeProvider>();
            // var tokenValidationParameters = new ValidationParameters(Encoding.UTF8.GetBytes(publicKeyPem));
            // tokenValidationParameters.ValidIssuer = "demo.energioprindelse.dk";
            // tokenValidationParameters.ValidAudience = "Users";
            // services.AddAuthentication().AddJwtBearer(AuthenticationScheme.TokenValidation, options =>
            // {
            //     options.MapInboundClaims = false;
            //     options.TokenValidationParameters = tokenValidationParameters;
            // });
        });
    }

    public async Task WithApiVersionDescriptionProvider(Func<IApiVersionDescriptionProvider, Task> withAction)
    {
        using var scope = Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IApiVersionDescriptionProvider>();
        await withAction(provider);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        if (string.IsNullOrWhiteSpace(ConnectionString))
            return host;

        var factory = host.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

        using var dbContext = factory.CreateDbContext();
        dbContext.Database.Migrate();

        var authenticationSchemeProvider = host.Services.GetService<IAuthenticationSchemeProvider>()!;
        authenticationSchemeProvider.RemoveScheme(AuthenticationScheme.B2CAuthenticationScheme);
        authenticationSchemeProvider.RemoveScheme(AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme);
        authenticationSchemeProvider.RemoveScheme(AuthenticationScheme.B2CMitICustomPolicyAuthenticationScheme);

        var b2CScheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            EnergyOrigin.TokenValidation.b2c.AuthenticationScheme.B2CAuthenticationScheme,
            EnergyOrigin.TokenValidation.b2c.AuthenticationScheme.B2CAuthenticationScheme,
            typeof(IntegrationTestAuthHandler));
        authenticationSchemeProvider.AddScheme(b2CScheme);

        var b2CMitIdScheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            EnergyOrigin.TokenValidation.b2c.AuthenticationScheme.B2CMitICustomPolicyAuthenticationScheme,
            EnergyOrigin.TokenValidation.b2c.AuthenticationScheme.B2CMitICustomPolicyAuthenticationScheme,
            typeof(IntegrationTestAuthHandler));
        authenticationSchemeProvider.AddScheme(b2CMitIdScheme);

        var b2CClientCredentialsScheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            EnergyOrigin.TokenValidation.b2c.AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme,
            EnergyOrigin.TokenValidation.b2c.AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme,
            typeof(IntegrationTestAuthHandler));
        authenticationSchemeProvider.AddScheme(b2CClientCredentialsScheme);

        return host;
    }

    public HttpClient CreateAuthenticatedClient(string sub, string tin = "11223344", string name = "Peter Producent",
        string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f", string apiVersion = ApiVersions.Version20230101)
    {
        client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(sub: sub, tin: tin, name: name, actor: actor));
        client.DefaultRequestHeaders.Add("EO_API_VERSION", apiVersion);

        return client;
    }

    public HttpClient CreateB2CAuthenticatedClient(string sub, string tin = "11223344", string name = "Peter Producent",
        string orgId = "d4f32241-442c-4043-8795-a4e6bf574e7f", string apiVersion = ApiVersions.Version20230101)
    {
        client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(sub: sub, tin: tin, name: name, orgId: orgId));
        client.DefaultRequestHeaders.Add("EO_API_VERSION", apiVersion);

        return client;
    }

    public HttpClient CreateWalletClient(string subject)
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(WalletUrl);
        var authentication = new AuthenticationHeaderValue("Bearer", GenerateToken(sub: subject));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authentication.Scheme, authentication.Parameter);

        return client;
    }

    public IBus GetMassTransitBus() => Services.GetRequiredService<IBus>();

    private string GenerateToken(
        string scope = "",
        string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f",
        string sub = "03bad0af-caeb-46e8-809c-1d35a5863bc7",
        string tin = "11223344",
        string cpn = "Producent A/S",
        string name = "Peter Producent",
        string issuer = "demo.energioprindelse.dk",
        string audience = "Users",
        string orgId = "03bad0af-caeb-46e8-809c-1d35a5863bc7")
    {
        var claims = new Dictionary<string, object>()
        {
            { UserClaimName.Scope, scope },
            { UserClaimName.ActorLegacy, actor },
            { UserClaimName.Actor, actor },
            { UserClaimName.Tin, tin },
            { UserClaimName.OrganizationName, cpn },
            { JwtRegisteredClaimNames.Name, name },
            { ClaimType.OrgIds, orgId },
            { ClaimType.OrgCvr, tin },
            { ClaimType.OrgName, cpn },
            { ClaimType.SubType, "User" },
            { UserClaimName.ProviderType, ProviderType.MitIdProfessional.ToString() },
            { UserClaimName.AllowCprLookup, "false" },
            { UserClaimName.AccessToken, "" },
            { UserClaimName.IdentityToken, "" },
            { UserClaimName.ProviderKeys, "" },
            { UserClaimName.OrganizationId, sub },
            { UserClaimName.MatchedRoles, "" },
            { UserClaimName.Roles, "" },
            { UserClaimName.AssignedRoles, "" }
        };

        var signedJwtToken = new TokenSigner(PrivateKey).Sign(
            sub,
            name,
            issuer,
            audience,
            null,
            60,
            claims
        );

        return signedJwtToken;
    }

    public async Task AddContract(string subject,
        string gsrn,
        DateTimeOffset startDate,
        MeteringPointType meteringPointType,
        MeasurementsWireMock measurementsWireMock,
        Technology technology = null!)
    {
        measurementsWireMock.SetupMeteringPointsResponse(gsrn: gsrn, type: meteringPointType, technology: technology);

        var client = CreateAuthenticatedClient(subject);
        var body = new { gsrn, startDate = startDate.ToUnixTimeSeconds() };
        var response = await client.PostAsJsonAsync("api/certificates/contracts", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    public void Start() => Server.Should().NotBeNull();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            client?.Dispose();
            foreach (var disposableChannel in disposableChannels)
            {
                disposableChannel.Dispose();
            }
        }


        base.Dispose(disposing);
    }
}
