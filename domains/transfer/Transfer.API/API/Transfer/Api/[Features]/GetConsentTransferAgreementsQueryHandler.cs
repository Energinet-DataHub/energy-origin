﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Dto.Responses;
using API.UnitOfWork;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;

namespace API.Transfer.Api._Features_;

public class GetConsentTransferAgreementsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetConsentTransferAgreementsQuery, TransferAgreementProposalOverviewResponse>
{

    public async Task<TransferAgreementProposalOverviewResponse> Handle(GetConsentTransferAgreementsQuery request, CancellationToken cancellationToken)
    {
        var transferAgreements =
            await unitOfWork.TransferAgreementRepo.GetTransferAgreementsList(request.OrganizationIds, cancellationToken);
        var transferAgreementProposals = await unitOfWork.TransferAgreementRepo.GetTransferAgreementProposals(request.OrganizationIds, cancellationToken);

        if (!transferAgreementProposals.Any() && !transferAgreements.Any())
        {
            return new TransferAgreementProposalOverviewResponse(new());
        }

        var transferAgreementDtos = transferAgreements
            .Select(x => new TransferAgreementProposalOverviewDto(x.Id, x.StartDate.EpochSeconds, x.EndDate?.EpochSeconds, x.SenderName.Value,
                x.SenderTin.Value, x.ReceiverTin.Value, TransferAgreementTypeMapper.MapCreateTransferAgreementType(x.Type),
                GetTransferAgreementStatusFromAgreement(x)))
            .ToList();

        var transferAgreementProposalDtos = transferAgreementProposals
            .Select(x => new TransferAgreementProposalOverviewDto(x.Id, x.StartDate.EpochSeconds, x.EndDate?.EpochSeconds, string.Empty,
                string.Empty, x.ReceiverCompanyTin?.Value, TransferAgreementTypeMapper.MapCreateTransferAgreementType(x.Type),
                GetTransferAgreementStatusFromProposal(x)))
            .ToList();

        transferAgreementProposalDtos.AddRange(transferAgreementDtos);

        return new TransferAgreementProposalOverviewResponse(transferAgreementProposalDtos);
    }

    private TransferAgreementStatus GetTransferAgreementStatusFromProposal(TransferAgreementProposal transferAgreementProposal)
    {
        var timespan = DateTimeOffset.UtcNow - transferAgreementProposal.CreatedAt.ToDateTimeOffset();

        return timespan.Days switch
        {
            >= 0 and <= 14 => TransferAgreementStatus.Proposal,
            _ => TransferAgreementStatus.ProposalExpired
        };
    }

    private TransferAgreementStatus GetTransferAgreementStatusFromAgreement(TransferAgreement transferAgreement)
    {
        if (transferAgreement.StartDate <= UnixTimestamp.Now() && transferAgreement.EndDate == null)
        {
            return TransferAgreementStatus.Active;
        }
        else if (transferAgreement.StartDate < UnixTimestamp.Now() && transferAgreement.EndDate != null &&
                 transferAgreement.EndDate > UnixTimestamp.Now())
        {
            return TransferAgreementStatus.Active;
        }

        return TransferAgreementStatus.Inactive;
    }

}

public record GetConsentTransferAgreementsQuery(IList<Guid> OrganizationIds) : IRequest<TransferAgreementProposalOverviewResponse>;

public record ConsentTransferAgreementProposalOverviewDto(
    Guid Id,
    long StartDate,
    long? EndDate,
    string? SenderName,
    string? SenderTin,
    string? ReceiverTin,
    TransferAgreementTypeDto Type,
    TransferAgreementStatus TransferAgreementStatus);


public record ConsentTransferAgreementProposalOverviewResponse(List<TransferAgreementProposalOverviewDto> Result);

