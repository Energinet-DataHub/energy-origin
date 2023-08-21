namespace API.ApiModels.Requests;

public record CreateTransferAgreement(
    long StartDate,
    long? EndDate,
    string ReceiverTin,
    string Base64EncodedWalletDepositEndpoint);
