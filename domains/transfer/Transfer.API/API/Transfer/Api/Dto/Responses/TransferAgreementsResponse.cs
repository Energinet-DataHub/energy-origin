using System;
using System.Collections.Generic;

namespace API.Transfer.Api.Dto.Responses;

public record TransferAgreementsResponse(List<TransferAgreementDto> Result);

public record TransferAgreementProposalOverviewResponse(List<TransferAgreementProposalOverviewDto> Result);

public record TransferAgreementProposalOverviewDto(
    Guid Id,
    long StartDate,
    long? EndDate,
    string? ReceiverCompanyTin,
    TransferAgreementStatus TransferAgreementStatus);

public enum TransferAgreementStatus
{
    Active,
    Inactive,
    Proposal
}

/*
public Guid SenderCompanyId { get; set; }
public string SenderCompanyTin { get; set; } = string.Empty;
public string SenderCompanyName { get; set; } = string.Empty;
[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
public DateTimeOffset CreatedAt { get; set; }
public DateTimeOffset StartDate { get; set; }
public DateTimeOffset? EndDate { get; set; }
public string? ReceiverCompanyTin { get; set; }
*/
