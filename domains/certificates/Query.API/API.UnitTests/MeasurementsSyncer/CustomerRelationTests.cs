using System;
using API.MeasurementsSyncer.Clients.DataHubFacade;
using Xunit;

namespace API.UnitTests.MeasurementsSyncer;

public class CustomerRelationTests
{
    [Fact]
    public void IsValidGsrn_WhenNoMatchingRelation_ReturnsFalse()
    {
        var relation = new CustomerRelation { MeteringPointId = Any.Gsrn().Value, ValidFromDate = DateTime.Now.AddHours(-1) };
        var gsrn = Any.Gsrn();

        var result = relation.IsValidGsrn(gsrn);

        Assert.False(result);
    }

    [Fact]
    public void IsValidGsrn_WhenMatchingRelationHasValidFromDate_ReturnsTrue()
    {
        var gsrn = Any.Gsrn();
        var relation = new CustomerRelation { MeteringPointId = gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) };

        var result = relation.IsValidGsrn(gsrn);

        Assert.True(result);
    }

    [Fact]
    public void IsValidGsrn_WhenMatchingRelationHasValidFromDateInFuture_ReturnsFalse()
    {
        var gsrn = Any.Gsrn();
        var relation = new CustomerRelation { MeteringPointId = gsrn.Value, ValidFromDate = DateTime.Now.AddHours(1) };

        var result = relation.IsValidGsrn(gsrn);

        Assert.False(result);
    }
}
