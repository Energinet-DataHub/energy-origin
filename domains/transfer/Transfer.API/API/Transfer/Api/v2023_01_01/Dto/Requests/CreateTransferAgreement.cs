namespace API.Transfer.Api.v2023_01_01.Dto.Requests;

public record CreateTransferAgreement(
    long StartDate,
    long? EndDate,
    string ReceiverTin,
    string Base64EncodedWalletDepositEndpoint);
