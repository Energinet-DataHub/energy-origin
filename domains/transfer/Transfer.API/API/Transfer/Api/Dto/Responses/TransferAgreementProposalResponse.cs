using System;

namespace API.Transfer.Api.Dto.Responses;

public record TransferAgreementProposalResponse(
    Guid Id,
    string SenderCompanyName,
    string? ReceiverTin,
    long StartDate,
    long? EndDate,
    TransferAgreementTypeDto Type);
