using System.Collections.Generic;
using System;

namespace API.MeasurementsSyncer.Clients.DataHubFacade;

public record ListMeteringPointForCustomerCaResponse
{
    public required List<CustomerRelation> Relations { get; init; }
    public required List<Rejection> Rejections { get; init; }
}

public class CustomerRelation
{
    public required string MeteringPointId { get; init; }
    public required DateTime ValidFromDate { get; init; }
}

public class Rejection
{
    public required string MeteringPointId { get; set; }
    public required string ErrorCode { get; set; }
    public required string ErrorDetailName { get; set; }
    public required string ErrorDetailValue { get; set; }
}
