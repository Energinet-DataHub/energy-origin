using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Cvr.Api._Features_.Internal;
using API.Cvr.Api.Clients.Cvr;
using API.Cvr.Api.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace API.UnitTests.Cvr.Api._Features_;

public class GetCvrCompanyQueryHandlerTests
{
    private readonly ICvrClient _cvrClient = Substitute.For<ICvrClient>();
    private readonly ILogger<GetCvrCompanyQueryHandler> _logger = Substitute.For<ILogger<GetCvrCompanyQueryHandler>>();

    private readonly GetCvrCompanyQueryHandler _sut;

    public GetCvrCompanyQueryHandlerTests()
    {
        _sut = new GetCvrCompanyQueryHandler(_cvrClient, _logger);
    }

    [Fact]
    public async Task WhenGettingCvrCompany_NoCompanyInformation_ReturnsNull()
    {
        // Arrange
        var query = new GetCvrCompanyQuery("12345678");

        _cvrClient.CvrNumberSearch(Arg.Any<CvrNumber[]>()).Returns(new Root
        {
            hits = new HitsRoot()
        });

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task WhenGettingCvrCompany_ExceptionWhenGettingCompanyInformation_ReturnsNull()
    {
        // Arrange
        var query = new GetCvrCompanyQuery("12345678");

        _cvrClient.CvrNumberSearch(Arg.Any<CvrNumber[]>()).ThrowsAsync(new InvalidOperationException());

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task WhenGettingCvrCompany_WithNoCompanyInformation_ReturnCompanyInformation()
    {
        // Arrange
        var cvr = "12345678";
        var query = new GetCvrCompanyQuery(cvr);

        var name = "Test Company";
        var city = "Test City";
        var zipCode = 1234;
        _cvrClient.CvrNumberSearch(Arg.Any<IEnumerable<CvrNumber>>()).Returns(new Root
        {
            hits = new HitsRoot
            {
                hits =
                [
                    new Hit
                    {
                        _source = new Source
                        {
                            Vrvirksomhed = new Vrvirksomhed
                            {
                                cvrNummer = 12345678, virksomhedMetadata = new VirksomhedMetadata
                                {
                                    nyesteNavn = new NyesteNavn
                                    {
                                        navn = name
                                    },
                                    nyesteBeliggenhedsadresse = new NyesteBeliggenhedsadresse
                                    {
                                        bynavn = city,
                                        postnummer = zipCode,
                                        vejnavn = "Test Street",
                                        husnummerFra = 1,
                                        bogstavFra = "A",
                                        etage = "2",
                                        sidedoer = "B"
                                    }
                                }
                            }
                        }
                    }
                ]
            }
        });

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cvr, result.Tin);
        Assert.Equal(name, result.Name);
        Assert.Equal(city, result.City);
        Assert.Equal(zipCode.ToString(), result.ZipCode);
        Assert.Equal("Test Street 1 A 2 B", result.Address);
    }
}
