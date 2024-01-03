using System.Collections.Generic;

namespace API.Transfer.Api.v2024_01_03.Dto.Responses;

public record TransferAgreementHistoryEntriesResponse(int totalCount, List<TransferAgreementHistoryEntryDto> items);
