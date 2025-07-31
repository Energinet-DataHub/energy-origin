using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Services;
using EnergyOrigin.Setup.Exceptions;
using MediatR;

namespace AdminPortal._Features_;

public class CreateContractCommandHandler(IContractService contractService, IAuthorizationService authorizationService)
    : IRequestHandler<CreateContractCommand, CreateContractResponse>
{
    public async Task<CreateContractResponse> Handle(CreateContractCommand command, CancellationToken cancellationToken)
    {
        var organizations = await authorizationService.GetOrganizationsAsync(cancellationToken);

        var validOrganization = organizations
            .Result
            .Any(x => x.OrganizationId == command.MeteringPointOwnerId && x.Tin.Equals(command.OrganizationTin, StringComparison.OrdinalIgnoreCase));

        if (!validOrganization)
        {
            throw new BusinessException("An invalid organization was supplied");
        }

        return await CreateContracts(command);
    }

    private async Task<CreateContractResponse> CreateContracts(CreateContractCommand command)
    {
        var createContracts = command.Contracts.Select(x => new CreateContract { Gsrn = x.Gsrn, StartDate = x.StartDate, EndDate = x.EndDate }).ToList();

        var request = new CreateContracts(createContracts, command.MeteringPointOwnerId, command.OrganizationTin, command.OrganizationName, command.IsTrial);
        var contractList = await contractService.CreateContracts(request);

        var createContractResponse = new CreateContractResponse
        {
            Contracts = [.. contractList.Result.Select(x => new CreateContractResponseItem { Created = x.Created, Gsrn = x.Gsrn, Id = x.Id })]
        };

        return createContractResponse;
    }
}

public class CreateContractItem
{
    public string Gsrn { get; init; } = "";
    public long StartDate { get; init; }
    public long? EndDate { get; set; }
}

public class CreateContractCommand : IRequest<CreateContractResponse>
{

    public required List<CreateContractItem> Contracts { get; set; }
    public Guid MeteringPointOwnerId { get; set; }
    public required string OrganizationTin { get; set; }
    public required string OrganizationName { get; set; }
    public bool IsTrial { get; set; }
}

public class CreateContractResponseItem
{
    public Guid Id { get; init; }
    public long Created { get; init; }
    public string Gsrn { get; init; } = string.Empty;
}

public class CreateContractResponse
{
    public required List<CreateContractResponseItem> Contracts { get; set; }
}
