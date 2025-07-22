using System;

namespace API.Transfer.Api.Common;

public class MeteringPointTypeHelper
{
    public static bool IsConsumption(string typeOfMeteringPoint)
    {
        return typeOfMeteringPoint.Trim().Equals("E17", StringComparison.OrdinalIgnoreCase);
    }
}
