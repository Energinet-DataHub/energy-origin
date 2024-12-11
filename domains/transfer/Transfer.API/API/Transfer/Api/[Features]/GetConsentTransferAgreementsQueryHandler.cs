using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Dto.Responses;
using API.Transfer.Api.Services;
using API.UnitOfWork;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;

namespace API.Transfer.Api._Features_;

public class GetConsentTransferAgreementsQueryHandler(IUnitOfWork unitOfWork, TransferAgreementStatusService transferAgreementStatusService) : IRequestHandler<GetConsentTransferAgreementsQuery, GetTransferAgreementQueryResult>
{

    public async Task<GetTransferAgreementQueryResult> Handle(GetConsentTransferAgreementsQuery request, CancellationToken cancellationToken)
    {
        var transferAgreements =
            await unitOfWork.TransferAgreementRepo.GetTransferAgreementsList(request.OrganizationIds, cancellationToken);
        var transferAgreementProposals = await unitOfWork.TransferAgreementRepo.GetTransferAgreementProposals(request.OrganizationIds, cancellationToken);

        if (!transferAgreementProposals.Any() && !transferAgreements.Any())
        {
            return new GetTransferAgreementQueryResult(new());
        }

        var transferAgreementDtos = transferAgreements
            .Select(x => new GetTransferAgreementQueryResultItem(x.Id, x.StartDate.EpochSeconds, x.EndDate?.EpochSeconds, x.SenderName.Value,
                x.SenderTin.Value, x.ReceiverTin.Value, TransferAgreementTypeMapper.MapCreateTransferAgreementType(x.Type),
                transferAgreementStatusService.GetTransferAgreementStatusFromAgreement(x)))
            .ToList();

        var transferAgreementProposalDtos = transferAgreementProposals
            .Select(x => new GetTransferAgreementQueryResultItem(x.Id, x.StartDate.EpochSeconds, x.EndDate?.EpochSeconds, string.Empty,
                string.Empty, x.ReceiverCompanyTin?.Value, TransferAgreementTypeMapper.MapCreateTransferAgreementType(x.Type),
                transferAgreementStatusService.GetTransferAgreementStatusFromProposal(x)))
            .ToList();

        transferAgreementProposalDtos.AddRange(transferAgreementDtos);

        return new GetTransferAgreementQueryResult(transferAgreementProposalDtos);
    }
}

public record GetConsentTransferAgreementsQuery(IList<Guid> OrganizationIds) : IRequest<GetTransferAgreementQueryResult>;

public record GetConsentTransferAgreementsQueryResultItem(
    Guid Id,
    long StartDate,
    long? EndDate,
    string? SenderName,
    string? SenderTin,
    string? ReceiverTin,
    TransferAgreementTypeDto Type,
    TransferAgreementStatus TransferAgreementStatus);


public record GetConsentTransferAgreementsQueryResult(List<GetTransferAgreementQueryResultItem> Result);
