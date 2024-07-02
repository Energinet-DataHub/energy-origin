using EnergyOrigin.IntegrationEvents.Events.Terms.V2;

namespace EnergyOrigin.IntegrationEvents.Tests.Events.Terms.V2;

public class OrgAcceptedTermsTests
{
    [Fact]
    public void can_set_values()
    {
        var id = Guid.NewGuid();
        var traceId = Guid.NewGuid().ToString();
        var created = DateTimeOffset.Now;
        var subjectId = Guid.NewGuid();
        var tin = "1234567890";
        var actor = Guid.NewGuid();
        var orgAcceptedTerms = new OrgAcceptedTerms(id, traceId, created, subjectId, tin, actor);

        Assert.Equal(id, orgAcceptedTerms.Id);
        Assert.Equal(traceId, orgAcceptedTerms.TraceId);
        Assert.Equal(created, orgAcceptedTerms.Created);
        Assert.Equal(subjectId, orgAcceptedTerms.SubjectId);
        Assert.Equal(tin, orgAcceptedTerms.Tin);
        Assert.Equal(actor, orgAcceptedTerms.Actor);
    }
}
