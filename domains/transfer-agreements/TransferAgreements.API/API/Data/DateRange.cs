using System;

namespace API.Data;

public class DateRange
{
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
}
