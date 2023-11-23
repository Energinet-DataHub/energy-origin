using System.Collections.Generic;

namespace API.Transfer.Api.v2023_11_23.Dto.Responses;

public record TransferAgreementHistoryEntriesResponse(int totalCount, List<TransferAgreementHistoryEntryDto> items);
