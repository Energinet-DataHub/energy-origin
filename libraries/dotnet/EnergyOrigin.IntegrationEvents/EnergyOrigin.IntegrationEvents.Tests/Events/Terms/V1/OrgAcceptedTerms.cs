using EnergyOrigin.IntegrationEvents.Events.Terms.V1;

namespace EnergyOrigin.IntegrationEvents.Tests.Events.Terms.V1;

public class OrgAcceptedTermsTests
{
    [Fact]
    public void can_set_values()
    {
        var subjectId = Guid.NewGuid();
        var tin = "1234567890";
        var actor = Guid.NewGuid();
        var orgAcceptedTerms = new OrgAcceptedTerms(subjectId, tin, actor);

        Assert.Equal(subjectId, orgAcceptedTerms.SubjectId);
        Assert.Equal(tin, orgAcceptedTerms.Tin);
        Assert.Equal(actor, orgAcceptedTerms.Actor);
    }
}
