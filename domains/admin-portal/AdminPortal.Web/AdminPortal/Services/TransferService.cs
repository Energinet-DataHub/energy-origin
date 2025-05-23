using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EnergyOrigin.Setup.Exceptions;

namespace AdminPortal.Services;

public interface ITransferService
{
    Task<CvrCompanyInformationDto> GetCompanyInformation(string tin);
    Task<CvrCompaniesListResponse> GetCompanies(List<string> cvrNumbers);
}

public class TransferService(HttpClient client) : ITransferService
{
    public async Task<CvrCompanyInformationDto> GetCompanyInformation(string tin)
    {
        var response = await client.GetAsync($"internal-cvr/companies/{tin}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ResourceNotFoundException(tin);
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CvrCompanyInformationDto>();
        return result ?? throw new InvalidOperationException("The API could not be reached or returned null.");
    }

    public async Task<CvrCompaniesListResponse> GetCompanies(List<string> cvrNumbers)
    {
        var request = new CvrCompaniesRequestDto
        {
            CvrNumbers = cvrNumbers
        };

        var response = await client.PostAsJsonAsync($"internal-cvr/companies", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CvrCompaniesListResponse>();
        return result ?? throw new InvalidOperationException("The API could not be reached or returned null.");
    }
}

public class CvrCompanyInformationDto
{
    public required string Tin { get; init; }
    public required string Name { get; init; }
    public required string City { get; set; }
    public required string ZipCode { get; set; }
    public required string Address { get; set; }
}

public class CvrCompaniesRequestDto
{
    public required List<string> CvrNumbers { get; set; }
}

public class CvrCompaniesListResponse
{
    public required List<CvrCompaniesInformationDto> Result { get; set; }
}

public class CvrCompaniesInformationDto
{
    public required string Tin { get; init; }
    public required string Name { get; init; }
}
