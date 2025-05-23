using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AdminPortal.Services;
using EnergyOrigin.Setup.Exceptions;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;

namespace AdminPortal.Tests.Services;

public class TransferServiceTest
{
    [Fact]
    public async Task WhenGettingCompanyInformation_WithBadRequestStatusCode_Throws()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var tin = "12345678";

        mockHttp.When($"http://localhost/internal-cvr/companies/{tin}")
            .Respond(HttpStatusCode.BadRequest, new StringContent(JsonConvert.SerializeObject(string.Empty)));

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("http://localhost");

        var transferService = new TransferService(client);

        // Act/Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => transferService.GetCompanyInformation(tin));
    }

    [Fact]
    public async Task WhenGettingCompanyInformation_WithNotFoundStatusCode_Throws()
    {
        // Arrange
        var mockResponse = new CvrCompanyInformationDto
        {
            Tin = "12345678",
            Name = "Test Company",
            City = "Test City",
            ZipCode = "1234",
            Address = "Test Address"
        };

        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When($"http://localhost/internal-cvr/companies/{mockResponse.Tin}")
            .Respond(HttpStatusCode.NotFound, new StringContent(JsonConvert.SerializeObject(mockResponse)));

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("http://localhost");

        var transferService = new TransferService(client);

        // Act/Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => transferService.GetCompanyInformation(mockResponse.Tin));
    }

    [Fact]
    public async Task WhenGettingCompanyInformation_WithSuccessStatusCode_ReturnsCompanyInformation()
    {
        // Arrange
        var mockResponse = new CvrCompanyInformationDto
        {
            Tin = "12345678",
            Name = "Test Company",
            City = "Test City",
            ZipCode = "1234",
            Address = "Test Address"
        };

        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When($"http://localhost/internal-cvr/companies/{mockResponse.Tin}")
            .Respond(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(mockResponse)));

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("http://localhost");

        var transferService = new TransferService(client);

        // Act
        var result = await transferService.GetCompanyInformation(mockResponse.Tin);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mockResponse.Tin, result.Tin);
        Assert.Equal(mockResponse.Name, result.Name);
        Assert.Equal(mockResponse.City, result.City);
        Assert.Equal(mockResponse.ZipCode, result.ZipCode);
        Assert.Equal(mockResponse.Address, result.Address);
    }

    [Fact]
    public async Task WhenGettingCompanies_WithNonSuccessStatusCode_Throws()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When($"http://localhost/internal-cvr/companies")
            .Respond(HttpStatusCode.BadRequest, new StringContent(JsonConvert.SerializeObject(string.Empty)));

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("http://localhost");

        var transferService = new TransferService(client);

        string tin = "1245678";

        // Act/Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => transferService.GetCompanies(new List<string> { tin }));
    }

    [Fact]
    public async Task WhenGettingCompanies_WithSuccessStatusCode_ReturnsCompanies()
    {
        // Arrange
        var tin = "1245678";
        var name = "Org 1";

        var mockResponse = new CvrCompaniesListResponse
        {
            Result =
            [
                new CvrCompaniesInformationDto
                {
                    Tin = tin,
                    Name = name
                }
            ]
        };

        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When($"http://localhost/internal-cvr/companies")
            .Respond(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(mockResponse)));

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("http://localhost");

        var transferService = new TransferService(client);

        // Act
        var result = await transferService.GetCompanies([tin]);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tin, result.Result[0].Tin);
        Assert.Equal(name, result.Result[0].Name);
    }
}
