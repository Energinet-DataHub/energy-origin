using System;

namespace API.ApiModels.Responses
{
    public record TransferAgreementResponse(Guid Id, long StartDate, long EndDate, string ReceiverTin);
}
