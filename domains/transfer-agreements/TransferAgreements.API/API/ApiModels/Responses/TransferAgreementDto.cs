using System;

namespace API.ApiModels.Responses
{
    public record TransferAgreementDto(
        Guid Id,
        long StartDate,
        long? EndDate,
        string SenderName,
        string SenderTin,
        string ReceiverTin);
}
