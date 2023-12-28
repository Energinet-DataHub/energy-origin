using System;
using System.Collections.Generic;

namespace API.Transfer.Api.v2023_11_23.Dto.Responses;

public record InternalTransferAgreementsDto(List<InternalTransferAgreementDto> Result);

public record InternalTransferAgreementDto(
    long StartDate,
    long? EndDate,
    string SenderId,
    string ReceiverTin,
    Guid ReceiverReference
);
