namespace API.ApiModels.Responses;

public record TransferAgreementAuditDto(
    long? EndDate,
    string? SenderName,
    string? ActorName,
    string SenderTin,
    string ReceiverTin);
