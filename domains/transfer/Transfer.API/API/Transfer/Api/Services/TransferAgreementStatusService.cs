using System;
using API.Transfer.Api.Dto.Responses;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;

namespace API.Transfer.Api.Services;

public class TransferAgreementStatusService
{
    public TransferAgreementStatus GetTransferAgreementStatusFromProposal(TransferAgreementProposal transferAgreementProposal)
    {
        var timespan = DateTimeOffset.UtcNow - transferAgreementProposal.CreatedAt.ToDateTimeOffset();

        return timespan.Days switch
        {
            >= 0 and <= 14 => TransferAgreementStatus.Proposal,
            _ => TransferAgreementStatus.ProposalExpired
        };
    }

    public TransferAgreementStatus GetTransferAgreementStatusFromAgreement(TransferAgreement transferAgreement)
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
