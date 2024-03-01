using System.Collections.Generic;

namespace API.Transfer.Api.Dto.Responses;

public record TransferAgreementHistoryEntriesResponse(int totalCount, List<TransferAgreementHistoryEntryDto> items);
