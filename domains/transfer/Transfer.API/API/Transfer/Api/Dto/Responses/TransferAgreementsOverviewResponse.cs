using System;
using System.Collections.Generic;

namespace API.Transfer.Api.Dto.Responses;


public record TransferAgreementOverviewDto(
    Guid Id,
    long StartDate,
    long? EndDate,
    string? SenderName,
    string? SenderTin,
    string? ReceiverTin,
    TransferAgreementTypeDto Type,
    TransferAgreementStatus TransferAgreementStatus);

public record TransferAgreementOverviewResponse(List<TransferAgreementOverviewDto> Result);
