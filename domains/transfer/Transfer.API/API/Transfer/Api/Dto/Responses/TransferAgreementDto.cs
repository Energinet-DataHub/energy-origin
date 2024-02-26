using System;

namespace API.Transfer.Api.Dto.Responses;

public record TransferAgreementDto(
    Guid Id,
    long StartDate,
    long? EndDate,
    string SenderName,
    string SenderTin,
    string ReceiverTin);
