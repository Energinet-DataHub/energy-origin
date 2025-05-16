using System.Threading.Tasks;
using AdminPortal.Dtos.Response;
using AdminPortal.Services;

namespace AdminPortal.Utilities.Local;

public class MockCertificatesService : ICertificatesService
{
    public Task<GetContractsForAdminPortalResponse> GetContractsHttpRequestAsync()
    {
        var response = new GetContractsForAdminPortalResponse(MockData.Contracts);
        return Task.FromResult(response);
    }
}
