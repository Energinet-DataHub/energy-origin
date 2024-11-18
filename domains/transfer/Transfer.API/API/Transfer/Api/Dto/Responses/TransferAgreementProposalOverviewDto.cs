using System;

namespace API.Transfer.Api.Dto.Responses;

public record TransferAgreementProposalOverviewDto(
    Guid Id,
    long StartDate,
    long? EndDate,
    string? SenderName,
    string? SenderTin,
    string? ReceiverTin,
    TransferAgreementTypeDto Type,
    TransferAgreementStatus TransferAgreementStatus);
