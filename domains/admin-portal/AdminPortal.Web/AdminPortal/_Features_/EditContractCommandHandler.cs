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

        var organization = organizations
            .Result
            .FirstOrDefault(x => x.OrganizationId == command.MeteringPointOwnerId);

        if (organization is null)
        {
            throw new BusinessException("An invalid metering point owner was supplied");
        }

        await EditContracts(command, organizationTin: organization.Tin, organizationName: organization.OrganizationName);
    }

    private async Task EditContracts(EditContractCommand command, string organizationTin, string organizationName)
    {
        var editContracts = command.Contracts.Select(x => new EditContractEndDate { Id = x.Id, EndDate = x.EndDate }).ToList();

        var request = new EditContracts(editContracts, command.MeteringPointOwnerId, organizationTin, organizationName);
        await contractService.EditContracts(request);
    }
}

public class EditContractItem
{
    public Guid Id { get; init; }
    public long? EndDate { get; set; }
}

public class EditContractCommand : IRequest
{
    public required List<EditContractItem> Contracts { get; set; }
    public Guid MeteringPointOwnerId { get; set; }
}
