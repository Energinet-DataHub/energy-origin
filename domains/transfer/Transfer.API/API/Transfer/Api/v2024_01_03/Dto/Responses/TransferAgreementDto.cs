using System;

namespace API.Transfer.Api.v2024_01_03.Dto.Responses;

public record TransferAgreementDto(
    Guid Id,
    long StartDate,
    long? EndDate,
    string SenderName,
    string SenderTin,
    string ReceiverTin);
