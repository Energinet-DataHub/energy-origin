using System.Collections.Generic;

namespace API.Claiming.Api.Dto.Response;

public record ClaimSubjectHistoryEntriesDto(List<ClaimSubjectHistoryEntryDto> Items);
