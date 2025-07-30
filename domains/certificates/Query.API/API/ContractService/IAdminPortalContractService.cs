using System.Threading;
using System.Threading.Tasks;
using API.Query.API.ApiModels.Requests.Internal;

namespace API.ContractService;

public interface IAdminPortalContractService
{
    public Task<CreateContractResult> Create(
            CreateContracts contracts,
            CancellationToken cancellationToken);

    Task<SetEndDateResult> SetEndDate(
            EditContracts contracts,
            CancellationToken cancellationToken);
}
