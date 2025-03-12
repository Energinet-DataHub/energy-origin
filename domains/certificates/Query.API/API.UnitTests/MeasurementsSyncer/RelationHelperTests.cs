using System;
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

    [Fact]
    public void HasValidRelationForGsrn_WhenNoMatchingRelation_ReturnsFalse()
    {
        var relations = new List<CustomerRelation>
        {
            new() { MeteringPointId = Any.Gsrn().Value, ValidFromDate = DateTime.Now.AddHours(-1) }
        };
        var gsrn = Any.Gsrn();

        var result = relations.HasValidRelationForGsrn(gsrn);

        Assert.False(result);
    }

    [Fact]
    public void HasValidRelationForGsrn_WhenMatchingRelationHasValidFromDate_ReturnsTrue()
    {
        var gsrn = Any.Gsrn();
        var relations = new List<CustomerRelation>
        {
            new() { MeteringPointId = gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) }
        };

        var result = relations.HasValidRelationForGsrn(gsrn);

        Assert.True(result);
    }

    [Fact]
    public void HasValidRelationForGsrn_WhenMatchingRelationHasValidFromDateInFuture_ReturnsFalse()
    {
        var gsrn = Any.Gsrn();
        var relations = new List<CustomerRelation>
        {
            new() { MeteringPointId = gsrn.Value, ValidFromDate = DateTime.Now.AddHours(1) }
        };

        var result = relations.HasValidRelationForGsrn(gsrn);

        Assert.False(result);
    }
}
