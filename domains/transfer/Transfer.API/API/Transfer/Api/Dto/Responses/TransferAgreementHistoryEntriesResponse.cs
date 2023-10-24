using System.Collections.Generic;

namespace API.Transfer.Api.Dto.Responses;

public record TransferAgreementHistoryEntriesResponse(List<TransferAgreementHistoryEntryDto> Result);
