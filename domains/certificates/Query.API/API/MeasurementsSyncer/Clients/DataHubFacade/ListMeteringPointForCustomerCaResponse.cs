using System.Collections.Generic;
using System;
using DataContext.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;

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

    public bool IsValidGsrn(Gsrn gsrn)
    {
        return MeteringPointId == gsrn.Value && UnixTimestamp.Create(ValidFromDate) < UnixTimestamp.Now();
    }
}

public class Rejection
{
    public required string MeteringPointId { get; set; }
    public required string ErrorCode { get; set; }
    public required string ErrorDetailName { get; set; }
    public required string ErrorDetailValue { get; set; }

    public bool IsLmc001Error()
    {
        return ErrorCode == "LMC-001";
    }
}
