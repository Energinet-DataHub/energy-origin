using System.Collections.Generic;

namespace API.Transfer.Api.v2023_01_01.Dto.Responses;

public record TransferAgreementHistoryEntriesResponse(List<TransferAgreementHistoryEntryDto> Result);
