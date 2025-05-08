using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AdminPortal.Services;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;

namespace AdminPortal.Tests.Services;

public class TransferServiceTest
{
    [Fact]
    public async Task WhenGettingCompanyInformation_WithNonSuccessStatusCode_Throws()
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
            .Respond(HttpStatusCode.BadRequest, new StringContent(JsonConvert.SerializeObject(mockResponse)));

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("http://localhost");

        var transferService = new TransferService(client);

        // Act/Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => transferService.GetCompanyInformation(mockResponse.Tin));
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
}
