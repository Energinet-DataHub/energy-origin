using System.Collections.Generic;
using System.Text.Json.Serialization;
using DataContext.ValueObjects;

namespace API.Query.API.ApiModels.Responses.Internal;

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
