using System;
using System.Threading;
using System.Threading.Tasks;
using API.Query.API.ApiModels.Requests;

namespace API.ContractService;

public interface IAdminPortalContractService
{
    Task<CreateContractResult> Create(CreateContracts contracts, Guid meteringPointOwnerId, CancellationToken cancellationToken);

    Task<SetEndDateResult> SetEndDate(EditContracts contracts, Guid meteringPointOwnerId, CancellationToken cancellationToken);
}
