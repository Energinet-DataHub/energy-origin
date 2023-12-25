using System.Collections.Generic;

namespace API.v2023_01_01.Dto.Responses;

public record UserActivityLogsResponse(int TotalCount, List<UserActivityLogDto> Items);
