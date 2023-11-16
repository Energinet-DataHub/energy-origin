namespace API.Transfer.Api.v2023_11_11.Dto.Requests;

public record CreateTransferAgreement(
    long StartDate,
    long? EndDate,
    string ReceiverTin,
    string Base64EncodedWalletDepositEndpoint);
