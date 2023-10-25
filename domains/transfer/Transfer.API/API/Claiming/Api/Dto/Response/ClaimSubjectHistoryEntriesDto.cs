using System.Collections.Generic;
using API.Claiming.Api.Models;

namespace API.Claiming.Api.Dto.Response;

public record ClaimSubjectHistoryEntriesDto(List<ClaimSubjectHistoryEntryDto> History);
