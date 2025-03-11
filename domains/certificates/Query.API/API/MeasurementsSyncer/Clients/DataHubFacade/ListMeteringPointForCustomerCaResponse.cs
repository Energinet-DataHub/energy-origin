using System.Collections.Generic;
using System;

namespace API.MeasurementsSyncer.Clients.DataHubFacade;

public record ListMeteringPointForCustomerCaResponse
{
    public required List<CustomerRelation> Result { get; init; }
}

public class CustomerRelation
{
    public required string MeteringPointId { get; init; }
    public required DateTime ValidFromDate { get; init; }
}
