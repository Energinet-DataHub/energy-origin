using System;

namespace API.Transfer.Api.Dto.Requests;

public record CreateTransferAgreement(
    long StartDate,
    long? EndDate,
    string ReceiverTin,
    string Base64EncodedWalletDepositEndpoint);

public record CreateTransferAgreementFromProposal(
    Guid TransferAgreementProposalId);
