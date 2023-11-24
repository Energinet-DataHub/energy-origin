using System;

namespace API.Transfer.Api.v2023_01_01.Dto.Responses;

public record TransferAgreementDto(
    Guid Id,
    long StartDate,
    long? EndDate,
    string SenderName,
    string SenderTin,
    string ReceiverTin);
