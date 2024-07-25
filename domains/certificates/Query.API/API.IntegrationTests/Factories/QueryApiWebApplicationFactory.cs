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
using API.IntegrationTests.Extensions;
using API.IntegrationTests.Mocks;
using API.MeasurementsSyncer;
using API.Query.API.Controllers;
using Asp.Versioning.ApiExplorer;
using Contracts;
using DataContext;
using DataContext.ValueObjects;
using EnergyOrigin.ActivityLog;
using EnergyOrigin.TokenValidation.b2c;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using FluentAssertions;
using Grpc.Net.Client;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AuthenticationScheme = EnergyOrigin.TokenValidation.b2c.AuthenticationScheme;
using Technology = API.ContractService.Clients.Technology;

namespace API.IntegrationTests.Factories;

public class QueryApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly List<GrpcChannel> disposableChannels = new();
    private HttpClient? client;
    public string ConnectionString { get; set; } = "";
    public string MeasurementsUrl { get; set; } = "http://foo";
    public string WalletUrl { get; set; } = "bar";
    public string StampUrl { get; set; } = "baz";
    public string RegistryName { get; set; } = "TestRegistry";
    public bool MeasurementsSyncEnabled { get; set; } = false;
    public Measurements.V1.Measurements.MeasurementsClient? measurementsClient { get; set; } = null;

    private string OtlpReceiverEndpoint { get; set; } = "http://foo";
    public RabbitMqOptions? RabbitMqOptions { get; set; }
    private byte[] PrivateKey { get; set; } = RsaKeyGenerator.GenerateTestKey();
    private byte[] B2CDummyPrivateKey { get; set; } = RsaKeyGenerator.GenerateTestKey();

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
        builder.UseSetting("MeasurementsSync:SleepType", "EveryThirdSecond");
        builder.UseSetting("IssuingContractCleanup:SleepTime", "00:00:03");
        builder.UseSetting("Wallet:Url", WalletUrl);
        builder.UseSetting("Stamp:Url", StampUrl);
        builder.UseSetting("Stamp:RegistryName", RegistryName);
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
            if (!MeasurementsSyncEnabled)
                services.Remove(services.First(s => s.ImplementationType == typeof(MeasurementsSyncerWorker)));

            if (measurementsClient != null)
            {
                services.Remove(services.First(s => s.ServiceType == typeof(Measurements.V1.Measurements.MeasurementsClient)));
                services.AddSingleton(measurementsClient);
            }
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

        ReplaceB2CAuthenticationSchemes(host);

        return host;
    }

    private static void ReplaceB2CAuthenticationSchemes(IHost host)
    {
        var authenticationSchemeProvider = host.Services.GetService<IAuthenticationSchemeProvider>()!;
        authenticationSchemeProvider.RemoveScheme(AuthenticationScheme.B2CAuthenticationScheme);
        authenticationSchemeProvider.RemoveScheme(AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme);
        authenticationSchemeProvider.RemoveScheme(AuthenticationScheme.B2CMitIDCustomPolicyAuthenticationScheme);

        var b2CScheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            AuthenticationScheme.B2CAuthenticationScheme,
            AuthenticationScheme.B2CAuthenticationScheme,
            typeof(IntegrationTestB2CAuthHandler));
        authenticationSchemeProvider.AddScheme(b2CScheme);

        var b2CMitIdScheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            AuthenticationScheme.B2CMitIDCustomPolicyAuthenticationScheme,
            AuthenticationScheme.B2CMitIDCustomPolicyAuthenticationScheme,
            typeof(IntegrationTestB2CAuthHandler));
        authenticationSchemeProvider.AddScheme(b2CMitIdScheme);

        var b2CClientCredentialsScheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme,
            AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme,
            typeof(IntegrationTestB2CAuthHandler));
        authenticationSchemeProvider.AddScheme(b2CClientCredentialsScheme);
    }

    public HttpClient CreateAuthenticatedClient(string sub, string tin = "11223344", string name = "Peter Producent",
        string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f", string apiVersion = ApiVersions.Version20230101)
    {
        client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateSelfSignedToken(sub: sub, tin: tin, name: name, actor: actor));
        client.DefaultRequestHeaders.Add("EO_API_VERSION", apiVersion);

        return client;
    }

    public HttpClient CreateB2CAuthenticatedClient(Guid sub, Guid orgId, string tin = "11223344", string name = "Peter Producent",
        string apiVersion = ApiVersions.Version20240515)
    {
        client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateB2CDummyToken(sub: sub.ToString(), tin: tin, name: name, orgId: orgId.ToString()));
        client.DefaultRequestHeaders.Add("EO_API_VERSION", apiVersion);

        return client;
    }

    public HttpClient CreateWalletClient(string subject)
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(WalletUrl);
        client.DefaultRequestHeaders.Remove(WalletServiceClientExtensions.WalletOwnerHeader);
        client.DefaultRequestHeaders.Add(WalletServiceClientExtensions.WalletOwnerHeader, subject);
        return client;
    }

    public IBus GetMassTransitBus() => Services.GetRequiredService<IBus>();

    private string GenerateSelfSignedToken(
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

    private string GenerateB2CDummyToken(
        string scope = "",
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
            { JwtRegisteredClaimNames.Name, name },
            { ClaimType.OrgIds, orgId },
            { ClaimType.OrgCvr, tin },
            { ClaimType.OrgName, cpn },
            { ClaimType.SubType, "User" },
            { UserClaimName.AccessToken, "" },
            { UserClaimName.IdentityToken, "" },
            { UserClaimName.ProviderKeys, "" },
        };

        var signedJwtToken = new TokenSigner(B2CDummyPrivateKey).Sign(
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
