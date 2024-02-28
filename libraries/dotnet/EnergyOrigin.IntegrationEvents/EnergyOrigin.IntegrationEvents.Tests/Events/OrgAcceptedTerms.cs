namespace EnergyOrigin.IntegrationEvents.Tests;

public class OrgAcceptedTermsTests
{
    [Fact]
    public void can_set_values()
    {
        var subjectId = Guid.NewGuid();
        var tin = "1234567890";
        var orgAcceptedTerms = new OrgAcceptedTerms(subjectId, tin);
        Assert.Equal(subjectId, orgAcceptedTerms.SubjectId);
        Assert.Equal(tin, orgAcceptedTerms.Tin);
    }
}
