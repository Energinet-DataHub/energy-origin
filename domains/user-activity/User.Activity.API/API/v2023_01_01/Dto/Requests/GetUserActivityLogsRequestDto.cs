using System;
using System.Collections.Generic;

namespace API.v2023_01_01.Dto.Requests;

public record GetUserActivityLogsRequestDto
{
    // This will be populated with the string representations of UserActivityType
    public List<string> Filter { get; init; } = [];
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int Offset { get; init; } = 0;
    public int Limit { get; init; } = 10;
}
