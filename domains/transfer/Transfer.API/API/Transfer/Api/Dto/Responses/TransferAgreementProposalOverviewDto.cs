using System;

namespace API.Transfer.Api.Dto.Responses;

public record TransferAgreementProposalOverviewDto(
    Guid Id,
    long StartDate,
    long? EndDate,
    long CreatedDate,
    string? ReceiverCompanyTin,
    TransferAgreementStatus TransferAgreementStatus);
