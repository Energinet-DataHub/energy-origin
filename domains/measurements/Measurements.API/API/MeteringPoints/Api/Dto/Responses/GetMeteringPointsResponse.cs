using System.Collections.Generic;

namespace API.MeteringPoints.Api.Dto.Responses;

public record GetMeteringPointsResponse(List<MeteringPoint> Result);
