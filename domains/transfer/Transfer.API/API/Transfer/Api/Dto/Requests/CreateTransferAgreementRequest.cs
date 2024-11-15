using System;

namespace API.Transfer.Api.Dto.Requests;

public record CreateTransferAgreementRequest(
    Guid ReceiverOrganizationId,
    long StartDate,
    long? EndDate,
    string ReceiverTin, // TODO: Detele once we get info from Auth 🐉
    string ReceiverName,// TODO: Detele once we get info from Auth 🐉
    string SenderTin,   // TODO: Detele once we get info from Auth 🐉
    string SenderName   // TODO: Detele once we get info from Auth 🐉
    );
