using System;

namespace API.Transfer.Api.Dto.Requests;

public record CreateTransferAgreementRequest(
    Guid ReceiverOrganizationId,
    Guid SenderOrganizationId,
    long StartDate,
    long? EndDate,
    CreateTransferAgreementType Type
);
