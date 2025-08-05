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
    : IRequestHandler<CreateContractCommand, CreateContractCommandResponse>
{
    public async Task<CreateContractCommandResponse> Handle(CreateContractCommand command, CancellationToken cancellationToken)
    {
        var organizations = await authorizationService.GetOrganizationsAsync(cancellationToken);

        var organization = organizations
            .Result
            .FirstOrDefault(x => x.OrganizationId == command.MeteringPointOwnerId);

        if (organization is null)
        {
            throw new BusinessException("An invalid metering point owner was supplied");
        }

        var isTrial = organization.Status.Equals("Trial", StringComparison.OrdinalIgnoreCase);
        return await CreateContracts(command, organizationTin: organization.Tin, organizationName: organization.OrganizationName, isTrial: isTrial);
    }

    private async Task<CreateContractCommandResponse> CreateContracts(CreateContractCommand command, string organizationTin, string organizationName, bool isTrial)
    {
        var createContracts = command.Contracts.Select(x => new CreateContract { Gsrn = x.Gsrn, StartDate = x.StartDate, EndDate = x.EndDate }).ToList();

        var request = new CreateContracts(createContracts, command.MeteringPointOwnerId, organizationTin, organizationName, isTrial);
        var contractList = await contractService.CreateContracts(request);

        var createContractResponse = new CreateContractCommandResponse
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

public class CreateContractCommand : IRequest<CreateContractCommandResponse>
{
    public required List<CreateContractItem> Contracts { get; set; }
    public Guid MeteringPointOwnerId { get; set; }
}

public class CreateContractResponseItem
{
    public Guid Id { get; init; }
    public long Created { get; init; }
    public string Gsrn { get; init; } = string.Empty;
}

public class CreateContractCommandResponse
{
    public required List<CreateContractResponseItem> Contracts { get; set; }
}


// TODO: CABOL - Do we want to return more info here?
// public static Contract CreateFrom(DataContext.Models.CertificateIssuingContract contract) =>
//     new()
//     {
//         Id = contract.Id,
//         GSRN = contract.GSRN,
//         StartDate = contract.StartDate.ToUnixTimeSeconds(),
//         EndDate = contract.EndDate?.ToUnixTimeSeconds(),
//         Created = contract.Created.ToUnixTimeSeconds(),
//         MeteringPointType = contract.MeteringPointType.ToMeteringPointTypeResponse(),
//         Technology = Technology.From(contract.Technology)
//     };
