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

public class GetCvrCompaniesQueryHandlerTests
{
    private readonly ICvrClient _cvrClient = Substitute.For<ICvrClient>();
    private readonly ILogger<GetCvrCompaniesQueryHandler> _logger = Substitute.For<ILogger<GetCvrCompaniesQueryHandler>>();

    private readonly GetCvrCompaniesQueryHandler _sut;

    public GetCvrCompaniesQueryHandlerTests()
    {
        _sut = new GetCvrCompaniesQueryHandler(_cvrClient, _logger);
    }

    [Fact]
    public async Task WhenGettingCompanies_NoCompanyInformation_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetCvrCompaniesQuery(["12345678"]);

        _cvrClient.CvrNumberSearch(Arg.Any<CvrNumber[]>()).Returns(new Root
        {
            hits = new HitsRoot()
        });

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result.Result);
    }

    [Fact]
    public async Task WhenGettingCompanies_ExceptionWhenGettingCompanyInformation_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetCvrCompaniesQuery(["12345678"]);

        _cvrClient.CvrNumberSearch(Arg.Any<CvrNumber[]>()).ThrowsAsync(new InvalidOperationException());

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result.Result);
    }

    [Fact]
    public async Task WhenGettingCompanies_WithNoCompanyInformation_ReturnCompanyInformation()
    {
        // Arrange
        var cvr = "12345678";
        var query = new GetCvrCompaniesQuery([cvr]);

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
        Assert.NotEmpty(result.Result);
        Assert.Equal(cvr, result.Result[0].Tin);
        Assert.Equal(name, result.Result[0].Name);
    }
}
