using Xunit;
using API.ContractService.Models;

namespace API.UnitTests.Query.API.ApiModels.Responses;

public class MeteringPointTests
{
    // NOTE: When MeteringPoints are returned from measurements the names of the properties are serialized as part of the JSON that is returned.
    // This test makes sure that we don't change the names on the receiving side, which would break the API contract.
    [Fact]
    public void MeteringPoint_ResponsePropertyNames_ShouldBeStable()
    {
        Assert.Equal("Gsrn", nameof(MeteringPoint.Gsrn));
        Assert.Equal("GridArea", nameof(MeteringPoint.GridArea));
        Assert.Equal("MeteringPointType", nameof(MeteringPoint.MeteringPointType));
        Assert.Equal("SubMeterType", nameof(MeteringPoint.SubMeterType));
        Assert.Equal("Address", nameof(MeteringPoint.Address));
        Assert.Equal("Technology", nameof(MeteringPoint.Technology));
        Assert.Equal("ConsumerCvr", nameof(MeteringPoint.ConsumerCvr));
        Assert.Equal("CanBeUsedForIssuingCertificates", nameof(MeteringPoint.CanBeUsedForIssuingCertificates));
        Assert.Equal("Capacity", nameof(MeteringPoint.Capacity));
        Assert.Equal("BiddingZone", nameof(MeteringPoint.BiddingZone));
    }
}
