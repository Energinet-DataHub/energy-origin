namespace API.Transfer.Api.Dto.Requests;

public record CreateTransferAgreement(
    long StartDate,
    long? EndDate,
    string ReceiverTin,
    string Base64EncodedWalletDepositEndpoint);
