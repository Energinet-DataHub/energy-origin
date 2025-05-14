using System;
using System.Threading.Tasks;
using AdminPortal.Services;

namespace AdminPortal.Utilities.Local;

public class MockTransferService : ITransferService
{
    public Task<CvrCompanyInformationDto> GetCompanyInformation(string tin)
    {
        if (MockData.CompanyInformation.TryGetValue(tin, out var cvrInformation))
        {
            return Task.FromResult(cvrInformation);
        }

        throw new InvalidOperationException($"No mock data found for TIN: {tin}");
    }
}
