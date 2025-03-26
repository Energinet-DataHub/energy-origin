using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AdminPortal.Dtos.Response;

public record GetContractsForAdminPortalResponseItem(
    string GSRN,
    string MeteringPointOwner,
    long Created,
    long StartDate,
    long? EndDate,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    MeteringPointType MeteringPointType
);

public record GetContractsForAdminPortalResponse(IEnumerable<GetContractsForAdminPortalResponseItem> Result);

public enum MeteringPointType
{
    Consumption,
    Production
}
