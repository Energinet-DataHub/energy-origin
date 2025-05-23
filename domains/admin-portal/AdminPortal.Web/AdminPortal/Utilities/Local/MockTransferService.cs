using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdminPortal.Services;
using EnergyOrigin.Setup.Exceptions;

namespace AdminPortal.Utilities.Local;

public class MockTransferService : ITransferService
{
    public Task<CvrCompanyInformationDto> GetCompanyInformation(string tin)
    {
        if (MockData.CompanyInformation.TryGetValue(tin, out var cvrInformation))
        {
            return Task.FromResult(cvrInformation);
        }

        // To force an unexpected error
        if (tin == "12121212")
        {
            throw new InvalidOperationException();
        }

        throw new ResourceNotFoundException($"No mock data found for TIN: {tin}");
    }

    public Task<CvrCompaniesListResponse> GetCompanies(List<string> cvrNumbers)
    {
        return Task.FromResult(MockData.GetCompanies());
    }
}
