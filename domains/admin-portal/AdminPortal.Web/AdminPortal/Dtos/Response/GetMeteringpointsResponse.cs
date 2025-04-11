using System.Collections.Generic;
using AdminPortal.Models;

namespace AdminPortal.Dtos.Response;

public record GetMeteringpointsResponse(List<GetMeteringPointsResponseItem> Result);

public record GetMeteringPointsResponseItem(string GSRN, MeteringPointType MeterType);
