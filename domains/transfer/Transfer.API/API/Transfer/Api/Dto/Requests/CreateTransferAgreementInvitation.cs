namespace API.Transfer.Api.Dto.Requests;

public record CreateTransferAgreementInvitation(long StartDate,
    long? EndDate,
    string ReceiverTin);
