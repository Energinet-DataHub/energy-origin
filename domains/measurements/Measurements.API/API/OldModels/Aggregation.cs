using System.ComponentModel;

namespace API.OldModels;

public enum Aggregation
{
    [Description("Year")]
    Year,
    [Description("Month")]
    Month,
    [Description("Day")]
    Day,
    [Description("Hour")]
    Hour,
    [Description("QuarterHour")]
    QuarterHour,
    [Description("Actual")]
    Actual,
    [Description("Total")]
    Total
}
