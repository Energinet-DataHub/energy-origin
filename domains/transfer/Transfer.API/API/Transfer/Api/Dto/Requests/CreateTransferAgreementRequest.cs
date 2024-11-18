using System;

namespace API.Transfer.Api.Dto.Requests;

public record CreateTransferAgreementRequest(
    Guid ReceiverOrganizationId,
    Guid SenderOrganizationId,
    long StartDate,
    long? EndDate,
    string ReceiverTin, // TODO: Delete once we get info from Auth 🐉
    string ReceiverName,// TODO: Delete once we get info from Auth 🐉
    string SenderTin,   // TODO: Delete once we get info from Auth 🐉
    string SenderName   // TODO: Delete once we get info from Auth 🐉
    );
