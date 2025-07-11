using API.MeteringPoints.Api.Dto.Responses;
using Xunit;

namespace Tests.MeteringPoints.Api.Responses;

public class MeteringPointTests
{
    [Fact]
    public void MeteringPoint_ResponsePropertyNames_ShouldBeStable()
    {
        Assert.Equal("GSRN", nameof(MeteringPoint.GSRN));
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
