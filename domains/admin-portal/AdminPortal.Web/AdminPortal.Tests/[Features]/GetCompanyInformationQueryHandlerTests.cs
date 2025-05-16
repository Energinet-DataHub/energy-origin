using System;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Features;
using AdminPortal.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AdminPortal.Tests._Features_;

public class GetCompanyInformationQueryHandlerTests
{
    private readonly ITransferService _transferService = Substitute.For<ITransferService>();

    private readonly ILogger<GetCompanyInformationQueryHandler> _logger =
        Substitute.For<ILogger<GetCompanyInformationQueryHandler>>();

    private readonly GetCompanyInformationQueryHandler _sut;

    public GetCompanyInformationQueryHandlerTests()
    {
        _sut = new GetCompanyInformationQueryHandler(_transferService, _logger);
    }

    [Fact]
    public async Task GivenNoCompanyInformation_WhenGettingCompanyInformation_ReturnsNull()
    {
        // Arrange
        var tin = "12345678";
        _transferService.GetCompanyInformation(tin).ThrowsAsync(new InvalidOperationException());

        var query = new GetCompanyInformationQuery(tin);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GivenCompanyInformation_WhenGettingCompanyInformation_ReturnsCompanyInformation()
    {
        // Arrange
        var tin = "12345678";
        var dto = new CvrCompanyInformationDto
        {
            Address = "Address",
            Tin = "Tin",
            City = "City",
            Name = "Name",
            ZipCode = "ZipCode"
        };

        _transferService.GetCompanyInformation(tin).Returns(dto);

        var query = new GetCompanyInformationQuery(tin);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(dto.Address, result!.Address);
        Assert.Equal(dto.Tin, result.Tin);
        Assert.Equal(dto.City, result.City);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.ZipCode, result.ZipCode);
    }
}
