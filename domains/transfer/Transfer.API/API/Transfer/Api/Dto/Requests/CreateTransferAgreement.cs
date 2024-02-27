using System;

namespace API.Transfer.Api.Dto.Requests;

public record CreateTransferAgreement(
    Guid TransferAgreementProposalId);
