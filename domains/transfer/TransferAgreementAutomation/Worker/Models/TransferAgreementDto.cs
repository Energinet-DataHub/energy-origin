using System;
using System.Collections.Generic;

namespace TransferAgreementAutomation.Worker.Models;

public record TransferAgreementsDto(List<TransferAgreementDto> Result);

public record TransferAgreementDto(
    long StartDate,
    long? EndDate,
    string SenderId,
    string ReceiverTin,
    string ReceiverReference
);
