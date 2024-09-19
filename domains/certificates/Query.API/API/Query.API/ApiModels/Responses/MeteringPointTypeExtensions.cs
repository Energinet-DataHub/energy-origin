using System;

namespace API.Query.API.ApiModels.Responses;

public static class MeteringPointTypeExtensions
{
    public static MeteringPointTypeResponse ToMeteringPointTypeResponse(this DataContext.ValueObjects.MeteringPointType meteringPointType)
    {
        return meteringPointType switch
        {
            DataContext.ValueObjects.MeteringPointType.Production => MeteringPointTypeResponse.Production,
            DataContext.ValueObjects.MeteringPointType.Consumption => MeteringPointTypeResponse.Consumption,
            _ => throw new ArgumentOutOfRangeException(nameof(meteringPointType), meteringPointType, null)
        };
    }
}
