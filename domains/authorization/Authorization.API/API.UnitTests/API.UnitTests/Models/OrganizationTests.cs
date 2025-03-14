using API.Models;
using API.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;

namespace API.UnitTests.Models;

public class OrganizationTests
{
    [Fact]
    public void Organization_WithValidData_CreatesSuccessfully()
    {
        var tin = Tin.Create("12345678");
        var organizationName = OrganizationName.Create("Test Organization");

        var organization = Organization.Create(tin, organizationName);

        organization.Should().NotBeNull();
        organization.Id.Should().NotBeEmpty();
        organization.Tin.Should().Be(tin);
    }

    [Fact]
    public void Organization_CanExist_WithoutAffiliations()
    {
        var tin = Tin.Create("12345678");
        var organizationName = OrganizationName.Create("Test Organization");

        var organization = Organization.Create(tin, organizationName);

        organization.Affiliations.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Organization_CanHave_Affiliations()
    {
        var tin = Tin.Create("12345678");
        var organizationName = OrganizationName.Create("Test Organization");

        var organization = Organization.Create(tin, organizationName);

        var idpUserId = IdpUserId.Create(Guid.NewGuid());
        var userName = UserName.Create("Test User");
        var user = User.Create(idpUserId, userName);

        var affiliation = Affiliation.Create(user, organization);

        organization.Affiliations.Should().Contain(affiliation);
    }

    [Fact]
    public void Organization_WhenCreated_HasNoTermsAccepted()
    {
        var tin = Tin.Create("12345678");
        var organizationName = OrganizationName.Create("Test Organization");

        var organization = Organization.Create(tin, organizationName);

        organization.TermsAccepted.Should().BeFalse();
        organization.TermsVersion.Should().BeNull();
        organization.TermsAcceptanceDate.Should().BeNull();
    }

    [Fact]
    public void AcceptTerms_ShouldUpdateTermsInformation()
    {
        var tin = Tin.Create("12345678");
        var organizationName = OrganizationName.Create("Test Organization");
        var organization = Organization.Create(tin, organizationName);
        var terms = Terms.Create(1);

        organization.AcceptTerms(terms);

        organization.TermsAccepted.Should().BeTrue();
        organization.TermsVersion.Should().Be(1);
        organization.TermsAcceptanceDate.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AcceptTerms_ShouldUpdateTermsVersion_WhenCalledMultipleTimes()
    {
        var tin = Tin.Create("12345678");
        var organizationName = OrganizationName.Create("Test Organization");
        var organization = Organization.Create(tin, organizationName);
        var termsV1 = Terms.Create(1);
        var termsV2 = Terms.Create(2);

        organization.AcceptTerms(termsV1);
        organization.AcceptTerms(termsV2);

        organization.TermsAccepted.Should().BeTrue();
        organization.TermsVersion.Should().Be(2);
    }

    [Fact]
    public void InvalidateTerms_ShouldSetTermsAcceptedToFalse()
    {
        var tin = Tin.Create("12345678");
        var organizationName = OrganizationName.Create("Test Organization");
        var organization = Organization.Create(tin, organizationName);
        var terms = Terms.Create(1);

        organization.AcceptTerms(terms);
        organization.InvalidateTerms();

        organization.TermsAccepted.Should().BeFalse();
        organization.TermsVersion.Should().Be(1);
        organization.TermsAcceptanceDate.Should().NotBeNull();
    }

    [Fact]
    public void InvalidateTerms_ShouldWorkEvenIfTermsNotPreviouslyAccepted()
    {
        var tin = Tin.Create("12345678");
        var organizationName = OrganizationName.Create("Test Organization");
        var organization = Organization.Create(tin, organizationName);

        organization.InvalidateTerms();

        organization.TermsAccepted.Should().BeFalse();
        organization.TermsVersion.Should().BeNull();
        organization.TermsAcceptanceDate.Should().BeNull();
    }
}

