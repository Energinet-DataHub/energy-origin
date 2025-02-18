using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AdminPortal.API.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;

namespace AdminPortal.IntegrationTests;

public class AggregationServiceTests
{
    [Fact]
    public async Task GetActiveContractsAsync_ReturnsSpecifiedNumberOfEntries()
    {
        var mockHandler = new MockHttpMessageHandler { MockEntryCount = 50 };
        await using var factory = new CustomWebApplicationFactory(mockHandler);

        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAggregationService>();

        var result = await service.GetActiveContractsAsync();

        Assert.Equal(50, result.Results.MeteringPoints.Count);
    }

    [Fact]
    public async Task GetActiveContractsAsync_HandlesApiFailureFromAuthorization()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SetFirstPartyApiResponse(HttpStatusCode.InternalServerError);

        await using var factory = new CustomWebApplicationFactory(mockHandler);
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAggregationService>();

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetActiveContractsAsync());
    }


    [Fact]
    public async Task GetActiveContractsAsync_HandlesApiFailureFromCertificates()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SetFirstPartyApiResponse(HttpStatusCode.InternalServerError);

        await using var factory = new CustomWebApplicationFactory(mockHandler);
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAggregationService>();

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetActiveContractsAsync());
    }

    [Fact]
    public async Task GetActiveContractsAsync_HandlesAuthenticationFailure()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SetTokenEndpointResponse(HttpStatusCode.Unauthorized);

        await using var factory = new CustomWebApplicationFactory(mockHandler);
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAggregationService>();

        await Assert.ThrowsAsync<MsalServiceException>(
            () => service.GetActiveContractsAsync());
    }

    [Fact]
    public async Task GetActiveContractsAsync_ExcludesContractsWithoutMatchingOrganization()
    {
        var mockHandler = new MockHttpMessageHandler();

        var validOrgId = Guid.NewGuid().ToString();
        mockHandler.SetFirstPartyApiResponse(HttpStatusCode.OK, new
        {
            Result = new[] {
                new {
                    OrganizationId = validOrgId,
                    OrganizationName = "Valid Org",
                    Tin = "12345678"
                }
            }
        });

        mockHandler.SetContractsApiResponse(HttpStatusCode.OK, new
        {
            Result = new[] {
                new {
                    GSRN = "VALID_GSRN",
                    MeteringPointOwner = validOrgId,
                    MeteringPointType = "Production",
                    Created = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds(),
                    StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    EndDate = (long?)null
                },
                new {
                    GSRN = "INVALID_GSRN",
                    MeteringPointOwner = "NON_EXISTENT_ORG",
                    MeteringPointType = "Consumption",
                    Created = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds(),
                    StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    EndDate = (long?)null
                }
            }
        });

        await using var factory = new CustomWebApplicationFactory(mockHandler);
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAggregationService>();

        var result = await service.GetActiveContractsAsync();

        Assert.Single(result.Results.MeteringPoints);
        Assert.Equal("VALID_GSRN", result.Results.MeteringPoints[0].GSRN);
    }

    [Fact]
    public async Task GetActiveContractsAsync_ExcludesOrganizationsWithoutMatchingContracts()
    {
        var mockHandler = new MockHttpMessageHandler();

        var org1Id = Guid.NewGuid().ToString();
        var org2Id = Guid.NewGuid().ToString();
        mockHandler.SetFirstPartyApiResponse(HttpStatusCode.OK, new
        {
            Result = new[] {
                new {
                    OrganizationId = org1Id,
                    OrganizationName = "Org 1",
                    Tin = "11111111"
                },
                new {
                    OrganizationId = org2Id,
                    OrganizationName = "Org 2",
                    Tin = "22222222"
                }
            }
        });

        mockHandler.SetContractsApiResponse(HttpStatusCode.OK, new
        {
            Result = new[] {
                new {
                    GSRN = "GSRN_1",
                    MeteringPointOwner = org1Id,
                    MeteringPointType = "Production",
                    Created = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds(),
                    StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    EndDate = (long?)null
                }
            }
        });

        await using var factory = new CustomWebApplicationFactory(mockHandler);
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAggregationService>();

        var result = await service.GetActiveContractsAsync();

        Assert.Single(result.Results.MeteringPoints);
        Assert.Equal("Org 1", result.Results.MeteringPoints[0].OrganizationName);
    }
}
