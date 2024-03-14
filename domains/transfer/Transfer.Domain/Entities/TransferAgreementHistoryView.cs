namespace Transfer.Domain.Entities;

public record TransferAgreementHistoryView(int totalCount, List<TransferAgreementHistoryEntry> items);
