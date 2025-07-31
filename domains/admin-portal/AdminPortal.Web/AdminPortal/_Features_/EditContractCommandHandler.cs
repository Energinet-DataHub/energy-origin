using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Services;
using EnergyOrigin.Setup.Exceptions;
using MediatR;

namespace AdminPortal._Features_;

public class EditContractCommandHandler(IContractService contractService, IAuthorizationService authorizationService)
    : IRequestHandler<EditContractCommand>
{
    public async Task Handle(EditContractCommand command, CancellationToken cancellationToken)
    {
        var organizations = await authorizationService.GetOrganizationsAsync(cancellationToken);

        var validOrganization = organizations.Result.Any(x => x.OrganizationId == command.MeteringPointOwnerId && x.Tin == command.OrganizationTin);
        if (!validOrganization)
        {
            throw new BusinessException("An invalid organization was supplied");
        }

        await EditContracts(command);
    }

    private async Task EditContracts(EditContractCommand command)
    {
        var editContracts = command.Contracts.Select(x => new EditContractEndDate { Id = x.Id, EndDate = x.EndDate }).ToList();

        var request = new EditContracts(editContracts, command.MeteringPointOwnerId, command.OrganizationTin, command.OrganizationName);
        await contractService.EditContracts(request);
    }
}

public class EditContractItem
{
    public required Guid Id { get; init; }
    public long? EndDate { get; set; }
}

public class EditContractCommand : IRequest
{

    public required List<EditContractItem> Contracts { get; set; }
    public Guid MeteringPointOwnerId { get; set; }
    public required string OrganizationTin { get; set; }
    public required string OrganizationName { get; set; }
}
