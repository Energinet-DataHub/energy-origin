using System;
using System.Linq;
using System.Threading.Tasks;
using AdminPortal.Services;

namespace AdminPortal.Utilities.Local;

public class MockContractService : IContractService
{
    public Task<ContractList> CreateContracts(CreateContracts request)
    {
        return Task.FromResult(new ContractList
        {
            Result =
            [
                new()
                {
                    Id = Guid.NewGuid(),
                    Gsrn = request.Contracts.First().Gsrn,
                    StartDate = request.Contracts.First().StartDate,
                    EndDate = request.Contracts.First().EndDate,
                    Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    MeteringPointType = MeteringPointTypeResponse.Production,
                    Technology = new AdminPortal.Services.Technology
                    {
                        AibFuelCode = "FuelCode",
                        AibTechCode = "TechCode"
                    }
                }
            ]
        });
    }

    public Task EditContracts(EditContracts request)
    {
        return Task.CompletedTask;
    }
}
