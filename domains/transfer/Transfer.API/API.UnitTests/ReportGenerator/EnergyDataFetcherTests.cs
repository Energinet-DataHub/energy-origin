using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.ReportGenerator.Infrastructure;
using API.Transfer.Api.Services;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace API.UnitTests.ReportGenerator;

public class EnergyDataFetcherTests
{
    private readonly IConsumptionService _consumptionService;
    private readonly IWalletClient _walletClient;
    private readonly EnergyDataFetcher _sut;

    public EnergyDataFetcherTests()
    {
        _consumptionService = Substitute.For<IConsumptionService>();
        _walletClient = Substitute.For<IWalletClient>();
        _sut = new EnergyDataFetcher(_consumptionService, _walletClient);
    }

    [Fact]
    public async Task GetAsync_WhenTrialOrganization_ThenOnlyTrialClaimsAreReturned()
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var isTrial = true;

        var consumptionHours = new List<ConsumptionHour>
        {
            new(10) { KwhQuantity = 100 }
        };

        var allClaims = new List<Claim>
        {
            CreateClaim(isTrial: true),
            CreateClaim(isTrial: false),
            CreateClaim(isTrial: true)
        };

        _consumptionService
            .GetTotalAndAverageHourlyConsumption(orgId, from, to, Arg.Any<CancellationToken>())
            .Returns((consumptionHours, consumptionHours));

        _walletClient
            .GetClaimsAsync(orgId.Value, from, to, TimeMatch.All, Arg.Any<CancellationToken>())
            .Returns(new ResultList<Claim>
            {
                Result = allClaims,
                Metadata = new PageInfo
                {
                    Count = allClaims.Count,
                    Offset = 0,
                    Limit = allClaims.Count,
                    Total = allClaims.Count
                }
            });

        var (_, _, claims) = await _sut.GetAsync(orgId, from, to, isTrial, CancellationToken.None);

        claims.Should().HaveCount(2);
        claims.Should().OnlyContain(claim => claim.IsTrialClaim());
    }

    [Fact]
    public async Task GetAsync_WhenNonTrialOrganization_ThenOnlyNonTrialClaimsAreReturned()
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var isTrial = false;

        var consumptionHours = new List<ConsumptionHour>
        {
            new(10) { KwhQuantity = 100 }
        };

        var allClaims = new List<Claim>
        {
            CreateClaim(isTrial: true),
            CreateClaim(isTrial: false),
            CreateClaim(isTrial: false)
        };

        _consumptionService
            .GetTotalAndAverageHourlyConsumption(orgId, from, to, Arg.Any<CancellationToken>())
            .Returns((consumptionHours, consumptionHours));

        _walletClient
            .GetClaimsAsync(orgId.Value, from, to, TimeMatch.All, Arg.Any<CancellationToken>())
            .Returns(new ResultList<Claim>
            {
                Result = allClaims,
                Metadata = new PageInfo
                {
                    Count = allClaims.Count,
                    Offset = 0,
                    Limit = allClaims.Count,
                    Total = allClaims.Count
                }
            });

        var (_, _, claims) = await _sut.GetAsync(orgId, from, to, isTrial, CancellationToken.None);

        claims.Should().HaveCount(2);
        claims.Should().OnlyContain(claim => !claim.IsTrialClaim());
    }

    [Fact]
    public async Task GetAsync_WhenNoMatchingClaimsExist_ThenEmptyListIsReturned()
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var isTrial = true;

        var consumptionHours = new List<ConsumptionHour>
        {
            new(10) { KwhQuantity = 100 }
        };

        var nonTrialClaims = new List<Claim>
        {
            CreateClaim(isTrial: false),
            CreateClaim(isTrial: false)
        };

        _consumptionService
            .GetTotalAndAverageHourlyConsumption(orgId, from, to, Arg.Any<CancellationToken>())
            .Returns((consumptionHours, consumptionHours));

        _walletClient
            .GetClaimsAsync(orgId.Value, from, to, TimeMatch.All, Arg.Any<CancellationToken>())
            .Returns(new ResultList<Claim>
            {
                Result = nonTrialClaims,
                Metadata = new PageInfo
                {
                    Count = nonTrialClaims.Count,
                    Offset = 0,
                    Limit = nonTrialClaims.Count,
                    Total = nonTrialClaims.Count
                }
            });

        var (_, _, claims) = await _sut.GetAsync(orgId, from, to, isTrial, CancellationToken.None);

        claims.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_WhenMixedClaimsExist_ThenCorrectFilteringIsApplied()
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var isTrial = true;

        var consumptionHours = new List<ConsumptionHour>
        {
            new(10) { KwhQuantity = 100 }
        };

        var trialClaim = CreateClaim(isTrial: true);

        _consumptionService
            .GetTotalAndAverageHourlyConsumption(orgId, from, to, Arg.Any<CancellationToken>())
            .Returns((consumptionHours, consumptionHours));

        _walletClient
            .GetClaimsAsync(orgId.Value, from, to, TimeMatch.All, Arg.Any<CancellationToken>())
            .Returns(new ResultList<Claim>
            {
                Result = new List<Claim> { trialClaim },
                Metadata = new PageInfo
                {
                    Count = 1,
                    Offset = 0,
                    Limit = 1,
                    Total = 1
                }
            });

        var (_, _, claims) = await _sut.GetAsync(orgId, from, to, isTrial, CancellationToken.None);

        claims.Should().HaveCount(1);
        claims.Should().OnlyContain(claim => claim.IsTrialClaim());
    }

    [Fact]
    public async Task GetAsync_WhenIsTrialAttributeIsMissing_ThenClaimIsTreatedAsNonTrial()
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var isTrial = false;

        var consumptionHours = new List<ConsumptionHour>
        {
            new(10) { KwhQuantity = 100 }
        };

        var claimWithoutTrialAttribute = new Claim
        {
            ClaimId = Guid.NewGuid(),
            Quantity = 100,
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ProductionCertificate = new ClaimedCertificate
            {
                FederatedStreamId = new FederatedStreamId { Registry = "test", StreamId = Guid.NewGuid() },
                Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                End = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                GridArea = "DK1",
                Attributes = new Dictionary<string, string>()
            },
            ConsumptionCertificate = new ClaimedCertificate
            {
                FederatedStreamId = new FederatedStreamId { Registry = "test", StreamId = Guid.NewGuid() },
                Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                End = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                GridArea = "DK1",
                Attributes = new Dictionary<string, string>()
            }
        };

        _consumptionService
            .GetTotalAndAverageHourlyConsumption(orgId, from, to, Arg.Any<CancellationToken>())
            .Returns((consumptionHours, consumptionHours));

        _walletClient
            .GetClaimsAsync(orgId.Value, from, to, TimeMatch.All, Arg.Any<CancellationToken>())
            .Returns(new ResultList<Claim>
            {
                Result = new List<Claim> { claimWithoutTrialAttribute },
                Metadata = new PageInfo
                {
                    Count = 1,
                    Offset = 0,
                    Limit = 1,
                    Total = 1
                }
            });

        var (_, _, claims) = await _sut.GetAsync(orgId, from, to, isTrial, CancellationToken.None);

        claims.Should().HaveCount(1);
        claims[0].Should().Be(claimWithoutTrialAttribute);
    }

    [Fact]
    public async Task GetAsync_WhenIsTrialAttributeHasDifferentValues_ThenOnlyTrueIsTreatedAsTrial()
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var isTrial = true;

        var consumptionHours = new List<ConsumptionHour>
        {
            new(10) { KwhQuantity = 100 }
        };

        var claimsWithDifferentValues = new List<Claim>
        {
            CreateClaimWithAttribute("true"), // Should be included (case-insensitive)
            CreateClaimWithAttribute("TRUE"), // Should be included (case-insensitive)
            CreateClaimWithAttribute("True"), // Should be included (case-insensitive)

            CreateClaimWithAttribute("false"), // Should be filtered out (case-insensitive)
            CreateClaimWithAttribute("FALSE"), // Should be filtered out (case-insensitive)
            CreateClaimWithAttribute("False"), // Should be filtered out (case-insensitive)

            CreateClaimWithAttribute("1"), // Should be filtered out (not "true")
            CreateClaimWithAttribute("yes"), // Should be filtered out (not "true")
            CreateClaimWithAttribute("") // Should be filtered out (not "true")
        };

        _consumptionService
            .GetTotalAndAverageHourlyConsumption(orgId, from, to, Arg.Any<CancellationToken>())
            .Returns((consumptionHours, consumptionHours));

        _walletClient
            .GetClaimsAsync(orgId.Value, from, to, TimeMatch.All, Arg.Any<CancellationToken>())
            .Returns(new ResultList<Claim>
            {
                Result = claimsWithDifferentValues,
                Metadata = new PageInfo
                {
                    Count = claimsWithDifferentValues.Count,
                    Offset = 0,
                    Limit = claimsWithDifferentValues.Count,
                    Total = claimsWithDifferentValues.Count
                }
            });

        var (_, _, claims) = await _sut.GetAsync(orgId, from, to, isTrial, CancellationToken.None);

        claims.Should().HaveCount(3);
        claims.Should().Contain(claimsWithDifferentValues[0]); // "true"
        claims.Should().Contain(claimsWithDifferentValues[1]); // "TRUE"
        claims.Should().Contain(claimsWithDifferentValues[2]); // "True"
    }

    private static Claim CreateClaim(bool isTrial = false)
    {
        return new Claim
        {
            ClaimId = Guid.NewGuid(),
            Quantity = 100,
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ProductionCertificate = new ClaimedCertificate
            {
                FederatedStreamId = new FederatedStreamId { Registry = "test", StreamId = Guid.NewGuid() },
                Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                End = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                GridArea = "DK1",
                Attributes = new Dictionary<string, string>
                {
                    { "IsTrial", isTrial.ToString() }
                }
            },
            ConsumptionCertificate = new ClaimedCertificate
            {
                FederatedStreamId = new FederatedStreamId { Registry = "test", StreamId = Guid.NewGuid() },
                Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                End = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                GridArea = "DK1",
                Attributes = new Dictionary<string, string>
                {
                    { "IsTrial", isTrial.ToString() }
                }
            }
        };
    }

    private static Claim CreateClaimWithAttribute(string isTrialValue)
    {
        return new Claim
        {
            ClaimId = Guid.NewGuid(),
            Quantity = 100,
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ProductionCertificate = new ClaimedCertificate
            {
                FederatedStreamId = new FederatedStreamId { Registry = "test", StreamId = Guid.NewGuid() },
                Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                End = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                GridArea = "DK1",
                Attributes = new Dictionary<string, string>
                {
                    { "IsTrial", isTrialValue }
                }
            },
            ConsumptionCertificate = new ClaimedCertificate
            {
                FederatedStreamId = new FederatedStreamId { Registry = "test", StreamId = Guid.NewGuid() },
                Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                End = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                GridArea = "DK1",
                Attributes = new Dictionary<string, string>
                {
                    { "IsTrial", isTrialValue }
                }
            }
        };
    }
}
