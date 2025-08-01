using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal._Features_;
using AdminPortal.Dtos.Response;
using AdminPortal.Models;
using AdminPortal.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AdminPortal.Tests._Features_;

public class GetMeteringPointsQueryHandlerTest
{
    private readonly IMeasurementsService _measurementsService = Substitute.For<IMeasurementsService>();
    private readonly IAuthorizationService _authorizationService = Substitute.For<IAuthorizationService>();
    private readonly ICertificatesService _certificatesService = Substitute.For<ICertificatesService>();

    [Fact]
    public async Task Handle_Query_ReturnsMultipleMeteringpoints()
    {
        var tin = "12345678";
        var tin1 = "87654321";
        _authorizationService.GetOrganizationsAsync(CancellationToken.None)
            .Returns(new GetOrganizationsResponse(new List<GetOrganizationsResponseItem>()
            {
                new(Guid.NewGuid(), "TestOrg", tin, "Normal"),
                new(Guid.NewGuid(), "TestOrg1", tin1, "Trial")
            }));

        _measurementsService.GetMeteringPointsHttpRequestAsync(Arg.Any<Guid>())
            .Returns(new GetMeteringpointsResponse(new List<GetMeteringPointsResponseItem>()
            {
                new("GSRN",
                    MeteringPointType.Production,
                    "982",
                    SubMeterType.Physical,
                    new Address("Some vej 123", null, null, "Aarhus C", "8000", "Denmark", "0751", "Aarhus"),
                    new Technology("T010000", "F01040100"),
                    "12345678",
                    true,
                    "1234567",
                    "DK1"),
                new("GSRN1",
                    MeteringPointType.Production,
                    "982",
                    SubMeterType.Physical,
                    new Address("Some vej 123", null, null, "Aarhus C", "8000", "Denmark", "0751", "Aarhus"),
                    new Technology("T010000", "F01040100"),
                    "12345678",
                    true,
                    "1234567",
                    "DK1"),
            }));

        _certificatesService.GetContractsHttpRequestAsync()
            .Returns(new GetContractsForAdminPortalResponse(new List<GetContractsForAdminPortalResponseItem>()
            {
                new("GSRN", "TestOrg", DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds(),
                    DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds(), null, MeteringPointType.Production)
            }));

        var handler = new GetMeteringPointsQueryHandler(
            _measurementsService,
            _authorizationService,
            _certificatesService,
            Substitute.For<ILogger<GetMeteringPointsQueryHandler>>()
        );
        var query = new GetMeteringPointsQuery(tin);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsType<GetMeteringPointsQueryResult>(result);
        Assert.NotEmpty(result.ViewModel.MeteringPoints);
        Assert.Equal("12345678", result.ViewModel.Tin);
        Assert.Equal("TestOrg", result.ViewModel.OrganizationName);
        Assert.Collection(result.ViewModel.MeteringPoints,
            item =>
            {
                Assert.Equal("GSRN", item.GSRN);
                Assert.Equal(MeteringPointType.Production, item.MeterType);
                Assert.Equal("DK1", item.BiddingZone);
                Assert.Equal("1234567", item.Capacity);
                Assert.Equal("982", item.GridArea);
                Assert.Equal("Solar", item.Technology);
                Assert.True(item.CanBeUsedForIssuingCertificates);
                Assert.True(item.ActiveContract);
            },
            item =>
            {
                Assert.Equal("GSRN1", item.GSRN);
                Assert.Equal(MeteringPointType.Production, item.MeterType);
                Assert.Equal("DK1", item.BiddingZone);
                Assert.Equal("1234567", item.Capacity);
                Assert.Equal("982", item.GridArea);
                Assert.Equal("Solar", item.Technology);
                Assert.True(item.CanBeUsedForIssuingCertificates);
                Assert.False(item.ActiveContract);
            });
    }

    [Fact]
    public async Task Handle_NoDataFetched_ReturnsEmptyResponse()
    {
        _authorizationService.GetOrganizationsAsync(CancellationToken.None)
            .Returns(new GetOrganizationsResponse([]));
        _measurementsService.GetMeteringPointsHttpRequestAsync(Arg.Any<Guid>())
            .Returns(new GetMeteringpointsResponse([]));
        _certificatesService.GetContractsHttpRequestAsync()
            .Returns(new GetContractsForAdminPortalResponse([]));

        var handler = new GetMeteringPointsQueryHandler(
            _measurementsService,
            _authorizationService,
            _certificatesService,
            Substitute.For<ILogger<GetMeteringPointsQueryHandler>>()
        );

        var query = new GetMeteringPointsQuery("12345678");
        var result = await handler.Handle(query, CancellationToken.None);
        Assert.Equal(string.Empty, result.ViewModel.Tin);
        Assert.Equal(string.Empty, result.ViewModel.OrganizationName);
        Assert.Empty(result.ViewModel.MeteringPoints);
    }

}
