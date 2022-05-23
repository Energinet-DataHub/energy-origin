using System.ComponentModel;

namespace API.Models;

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
    [Description("Quarter")]
    Quarter,
    [Description("Actual")]
    Actual
}