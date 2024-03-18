using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API;
using API.MeteringPoints.Api;
using API.MeteringPoints.Api.Dto.Responses;
using API.MeteringPoints.Api.Models;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;
using Testcontainers.PostgreSql;
using Tests.Extensions;
using Tests.TestContainers;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace Tests.MeteringPoints.Api;

public class MeteringPointControllerTests : IClassFixture<CustomMeterPointWebApplicationFactory<Startup>>, IClassFixture<PostgresContainer>
{
    private readonly CustomMeterPointWebApplicationFactory<Startup> _factory;

    public MeteringPointControllerTests(CustomMeterPointWebApplicationFactory<Startup> factory, PostgresContainer postgresContainer)

    {
        factory.ConnectionString = postgresContainer.ConnectionString;
        _factory = factory;
        _factory.Start();
    }

    [Fact]
    public async Task Unauthorized()
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync("api/measurements/meteringpoints");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMeteringPoints()
    {
        var mockedResponse = new Meteringpoint.V1.MeteringPointsResponse
        {
            MeteringPoints =
            {
                new Meteringpoint.V1.MeteringPoint
                {
                    MeteringPointId = "1234567890123456",
                    TypeOfMp = "E17",
                    SubtypeOfMp = "D01",
                    StreetName = "Street",
                    BuildingNumber = "1",
                    FloorId = "1",
                    RoomId = "1",
                    CityName = "City",
                    Postcode = "1234",
                    AssetType = "E17"
                }
            }
        };
        var clientMock = _factory.Services.GetRequiredService<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();
        clientMock.GetOwnedMeteringPointsAsync(Arg.Any<Meteringpoint.V1.OwnedMeteringPointsRequest>())
            .Returns(mockedResponse);

        var subject = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(subject.ToString());

        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_factory.ConnectionString)
            .Options;
        var dbContext = new ApplicationDbContext(contextOptions);
        dbContext.Database.EnsureCreated();

        dbContext.Relations.Add(new RelationDto()
        {
            SubjectId = subject,
            Status = RelationStatus.Created,
            Actor = Guid.NewGuid()
        });
        dbContext.SaveChanges();

        var response = await client.GetFromJsonAsync<GetMeteringPointsResponse>("api/measurements/meteringpoints");

        response.Should().NotBeNull();
        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(response, settings);
        response!.Result.First().SubMeterType.Should()
            .Be(MeteringPoint.GetSubMeterType(mockedResponse.MeteringPoints.First().SubtypeOfMp));
        response.Result.First().Type.Should()
            .Be(MeteringPoint.GetMeterType(mockedResponse.MeteringPoints.First().TypeOfMp));
    }

    [Fact]
    public async Task GetMeteringPoints_GivenChildMp_ExpectChildMpOmitted()
    {
        var childTypeOfMp = "D01";

        var mockedResponse = new Meteringpoint.V1.MeteringPointsResponse
        {
            MeteringPoints =
            {
                new Meteringpoint.V1.MeteringPoint
                {
                    MeteringPointId = "1234567890123456",
                    TypeOfMp = "E17",
                    SubtypeOfMp = "D01",
                    StreetName = "Street",
                    BuildingNumber = "1",
                    FloorId = "1",
                    RoomId = "1",
                    CityName = "City",
                    Postcode = "1234",
                    AssetType = "E17"
                },
                new Meteringpoint.V1.MeteringPoint
                {
                    MeteringPointId = "1234567890123457",
                    TypeOfMp = childTypeOfMp,
                    SubtypeOfMp = "D01",
                    StreetName = "Street",
                    BuildingNumber = "1",
                    FloorId = "1",
                    RoomId = "1",
                    CityName = "City",
                    Postcode = "1234",
                    AssetType = "E17"
                }
            }
        };
        var clientMock = _factory.Services.GetRequiredService<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();

        clientMock.GetOwnedMeteringPointsAsync(Arg.Any<Meteringpoint.V1.OwnedMeteringPointsRequest>())
            .Returns(mockedResponse);

        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var response = await client.GetFromJsonAsync<GetMeteringPointsResponse>("api/measurements/meteringpoints");

        response.Should().NotBeNull();
        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(response, settings);
        response!.Result.First().SubMeterType.Should()
            .Be(MeteringPoint.GetSubMeterType(mockedResponse.MeteringPoints.First().SubtypeOfMp));
        response.Result.First().Type.Should()
            .Be(MeteringPoint.GetMeterType(mockedResponse.MeteringPoints.First().TypeOfMp));
    }

    [Theory]
    [InlineData("streetName", "", "", "")]
    [InlineData("streetName", "buildingNo", "", "")]
    [InlineData("streetName", "buildingNo", "floor", "")]
    [InlineData("streetName", "buildingNo", "", "room")]
    [InlineData("streetName", "", "floor", "room")]
    [InlineData("streetName", "", "floor", "")]
    [InlineData("streetName", "", "", "room")]
    [InlineData("", "buildingNo", "floor", "room")]
    [InlineData("", "buildingNo", "", "room")]
    [InlineData("", "buildingNo", "floor", "")]
    [InlineData("", "", "floor", "room")]
    [InlineData("", "", "floor", "")]
    [InlineData("", "", "", "room")]
    [InlineData("", "", "", "")]
    [InlineData("streetName", "buildingNo", "floor", "room")]
    [InlineData("streetName", "    buildingNo", "floor", "room")]
    [InlineData("streetName", "buildingNo", "floor", "    room")]
    [InlineData("    streetName", "buildingNo", "floor", "room")]
    [InlineData("    streetName", "    buildingNo", "    floor", "    room")]
    [InlineData("streetName", "buildingNo", "    floor", "room")]
    public async Task EmptyAddressInformation_GetMeteringPoint_AddressLinesWithoutWhiteSpace(string streetName,
        string buildingNumber, string floor, string room)
    {
        var clientMock = _factory.Services.GetRequiredService<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();
        clientMock.GetOwnedMeteringPointsAsync(Arg.Any<Meteringpoint.V1.OwnedMeteringPointsRequest>())
            .Returns(new Meteringpoint.V1.MeteringPointsResponse
            {
                MeteringPoints =
                {
                    new Meteringpoint.V1.MeteringPoint
                    {
                        MeteringPointId = "1234567890123456",
                        TypeOfMp = "E17",
                        SubtypeOfMp = "D01",
                        StreetName = streetName,
                        BuildingNumber = buildingNumber,
                        FloorId = floor,
                        RoomId = room,
                        CityName = "City",
                        Postcode = "1234",
                        AssetType = "E17"
                    }
                }
            });


        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var response = await client.GetFromJsonAsync<GetMeteringPointsResponse>("api/measurements/meteringpoints");

        response.Should().NotBeNull();

        response!.Result.First().Address.Address1.Trim().Should().Be(response.Result.First().Address.Address1);
        response.Result.First().Address.Address2!.Trim().Should().Be(response.Result.First().Address.Address2);
        response.Result.First().Address.Address1.Should().NotContain("  ");
        response.Result.First().Address.Address2.Should().NotContain("  ");
    }
}

public class CustomMeterPointWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    public string ConnectionString { get; set; } = "";

    public byte[] PrivateKey { get; set; } = RsaKeyGenerator.GenerateTestKey();

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

        var mockClient = Substitute.For<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();

        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Postgres", ConnectionString);
        builder.UseSetting("TokenValidation:PublicKey", publicKeyBase64);
        builder.UseSetting("TokenValidation:Issuer", "demo.energioprindelse.dk");
        builder.UseSetting("TokenValidation:Audience", "Users");
        builder.ConfigureTestServices(x => x.AddSingleton(mockClient));
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        if (string.IsNullOrWhiteSpace(ConnectionString))
            return host;

        var factory = host.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

        using var dbContext = factory.CreateDbContext();
        dbContext.Database.Migrate();

        return host;
    }
    public void Start() => Server.Should().NotBeNull();
    public HttpClient CreateUnauthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("EO_API_VERSION", ApiVersions.Version20240110);
        return client;
    }
    public HttpClient CreateAuthenticatedClient(string sub, string tin = "11223344", string name = "Peter Producent",
            string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f", string apiVersion = ApiVersions.Version20240110)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(sub: sub, tin: tin, name: name, actor: actor));
        client.DefaultRequestHeaders.Add("EO_API_VERSION", apiVersion);
        return client;
    }

    private string GenerateToken(
        string scope = "",
        string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f",
        string sub = "03bad0af-caeb-46e8-809c-1d35a5863bc7",
        string tin = "11223344",
        string cpn = "Producent A/S",
        string name = "Peter Producent",
        string issuer = "demo.energioprindelse.dk",
        string audience = "Users")
    {
        var claims = new Dictionary<string, object>()
            {
                { UserClaimName.Scope, scope },
                { UserClaimName.ActorLegacy, actor },
                { UserClaimName.Actor, actor },
                { UserClaimName.Tin, tin },
                { UserClaimName.OrganizationName, cpn },
                { JwtRegisteredClaimNames.Name, name },
                { UserClaimName.ProviderType, ProviderType.MitIdProfessional.ToString()},
                { UserClaimName.AllowCprLookup, "false"},
                { UserClaimName.AccessToken, ""},
                { UserClaimName.IdentityToken, ""},
                { UserClaimName.ProviderKeys, ""},
                { UserClaimName.OrganizationId, sub},
                { UserClaimName.MatchedRoles, ""},
                { UserClaimName.Roles, ""},
                { UserClaimName.AssignedRoles, ""}
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
}
