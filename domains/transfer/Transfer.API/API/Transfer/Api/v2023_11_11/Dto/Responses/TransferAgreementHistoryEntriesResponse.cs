using System.Collections.Generic;

namespace API.Transfer.Api.v2023_11_11.Dto.Responses;

public record TransferAgreementHistoryEntriesResponse(List<TransferAgreementHistoryEntryDto> Result);
