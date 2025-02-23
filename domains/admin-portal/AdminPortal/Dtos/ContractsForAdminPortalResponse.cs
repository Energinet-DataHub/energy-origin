using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AdminPortal.Dtos;

public record ContractsForAdminPortalResponseItem(
    string GSRN,
    string MeteringPointOwner,
    long Created,
    long StartDate,
    long? EndDate,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    MeteringPointType MeteringPointType
);

public record ContractsForAdminPortalResponse(IEnumerable<ContractsForAdminPortalResponseItem> Result);

public enum MeteringPointType
{
    Consumption,
    Production
}
