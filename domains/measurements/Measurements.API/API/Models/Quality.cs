using System.ComponentModel;

namespace API.Models;

public enum Quality
{
    [Description("Measured")]
    Measured = 1,
    [Description("Estimated")]
    Estimated = 2
}
