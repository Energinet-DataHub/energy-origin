using System.Collections.Generic;
using API.MeasurementsSyncer;
using API.MeasurementsSyncer.Clients.DataHubFacade;
using Xunit;

namespace API.UnitTests.MeasurementsSyncer;
public class RelationHelperTests
{
    [Fact]
    public void HasValidRelationForGsrn_WhenNoRelations_ReturnsFalse()
    {
        var relations = new List<CustomerRelation>();
        var gsrn = Any.Gsrn();

        var result = relations.HasValidRelationForGsrn(gsrn);

        Assert.False(result);
    }
}
