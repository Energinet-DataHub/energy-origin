using System;
using System.Collections.Generic;

namespace TransferAgreementAutomation.Worker.Models;

public record TransferAgreementsDto(List<TransferAgreementDto> Result);

public record TransferAgreementDto(
    Guid Id,
    long StartDate,
    long? EndDate,
    string SenderName,
    string SenderTin,
    string SenderId,
    string ReceiverTin,
    string ReceiverReference
);
