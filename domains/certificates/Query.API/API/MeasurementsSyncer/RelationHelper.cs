using System;
using System.Collections.Generic;
using System.Linq;
using API.MeasurementsSyncer.Clients.DataHubFacade;
using DataContext.ValueObjects;

namespace API.MeasurementsSyncer;

public static class RelationHelper
{
    public static bool HasValidRelationForGsrn(this List<CustomerRelation> relations, Gsrn gsrn)
    {
        return relations.Any(x => x.MeteringPointId == gsrn.Value && x.ValidFromDate.ToUniversalTime() > DateTime.UtcNow);
    }
}
