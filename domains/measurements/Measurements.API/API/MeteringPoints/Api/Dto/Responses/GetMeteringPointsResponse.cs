using System.Collections.Generic;
using API.MeteringPoints.Api.Models;

namespace API.MeteringPoints.Api.Dto.Responses;

public record GetMeteringPointsResponse(List<MeteringPoint> Result, RelationStatus Status);
