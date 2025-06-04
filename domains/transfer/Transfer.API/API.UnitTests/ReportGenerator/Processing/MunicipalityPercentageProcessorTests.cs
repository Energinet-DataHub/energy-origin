using System;
using System.Collections.Generic;
using System.Linq;
using API.ReportGenerator.Processing;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using Xunit;

namespace API.UnitTests.ReportGenerator.Processing;

public class MunicipalityPercentageProcessorTests
{
    [Fact]
    public void Calculate()
    {
        var claims123 = GenerateClaims(10, "123");
        var claims124 = GenerateClaims(10, "124");
        var claims125 = GenerateClaims(10, "125");
        var claimsNull = GenerateClaims(10, null);

        var sut = new MunicipalityPercentageProcessor();

        var claims = new List<Claim>();
        claims.AddRange(claims123);
        claims.AddRange(claims124);
        claims.AddRange(claims125);
        claims.AddRange(claimsNull);

        var result = sut.Calculate(claims);

        Assert.NotNull(result);
        Assert.Equal(4, result.Count);
        Assert.Equal([25, 25, 25, 25], result.Select(x => x.Percentage));
        Assert.Equal(["123", "124", "125", null], result.Select(x => x.Municipality));
    }

    private List<Claim> GenerateClaims(int count, string? municipalityCode)
    {
        var claims = new List<Claim>();
        var attributes = new Dictionary<string, string>();
        if (municipalityCode != null)
            attributes.Add("municipality_code", municipalityCode);
        else
        {
            attributes = [];
        }

        for (int i = 0; i < count; i++)
        {
            var claim = new Claim
            {
                ClaimId = Guid.NewGuid(),
                Quantity = 123 + (uint)i,
                UpdatedAt = DateTimeOffset.Now.ToUnixTimeSeconds(),
                ConsumptionCertificate = new ClaimedCertificate
                {
                    Start = DateTimeOffset.Now.ToUnixTimeSeconds(),
                    End = DateTimeOffset.Now.AddHours(1).ToUnixTimeSeconds(),
                    FederatedStreamId = new FederatedStreamId
                    {
                        Registry = "Narnia",
                        StreamId = Guid.NewGuid()
                    },
                    GridArea = "DK1",
                    Attributes = attributes
                },
                ProductionCertificate = new ClaimedCertificate
                {
                    Start = DateTimeOffset.Now.ToUnixTimeSeconds(),
                    End = DateTimeOffset.Now.AddHours(1).ToUnixTimeSeconds(),
                    FederatedStreamId = new FederatedStreamId
                    {
                        Registry = "Narnia",
                        StreamId = Guid.NewGuid()
                    },
                    GridArea = "DK1",
                    Attributes = attributes
                }
            };
            claims.Add(claim);
        }

        return claims;
    }
}
