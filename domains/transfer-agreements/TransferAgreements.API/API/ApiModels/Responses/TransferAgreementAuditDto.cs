namespace API.ApiModels.Responses;

public record TransferAgreementAuditDto(
    long StartDate,
    long? EndDate,
    string? SenderName,
    string? ActorName,
    string SenderTin,
    string ReceiverTin);
