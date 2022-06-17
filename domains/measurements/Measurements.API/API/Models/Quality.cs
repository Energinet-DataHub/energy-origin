using System.ComponentModel;

namespace API.Models;

public enum Quality
{
    [Description("Measured")]
    Measured = 10,
    [Description("Revised")]
    Revised = 20,
    [Description("Calculated")]
    Calculated = 30,
    [Description("Estimated")]
    Estimated = 40,
}
