using API.Models;
using API.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup.Exceptions;
using FluentAssertions;
using System;
using System.Threading;
using Xunit;

namespace API.UnitTests.Models;

public class OrganizationTests
{
    [Fact]
    public void AcceptTerms_Twice_OverwritesPreviousTerms()
    {
        var org = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test"));
        var v1 = Terms.Create(1);
        var v2 = Terms.Create(2);

        org.AcceptTerms(v1, isWhitelisted: true);
        var timestamp1 = org.TermsAcceptanceDate;
        Thread.Sleep(10); // ensure time difference
        org.AcceptTerms(v2, isWhitelisted: true);

        org.TermsAccepted.Should().BeTrue();
        org.TermsVersion.Should().Be(2);
        org.TermsAcceptanceDate.Should().NotBe(timestamp1);
    }

    [Fact]
    public void Organization_WithValidData_CreatesSuccessfully()
    {
        var tin = Tin.Create("12345678");
        var organizationName = OrganizationName.Create("Test Organization");

        var organization = Organization.Create(tin, organizationName);

        organization.Should().NotBeNull();
        organization.Id.Should().NotBeEmpty();
        organization.Tin.Should().Be(tin);
        organization.Status.Should().Be(OrganizationStatus.Normal);
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

        organization.AcceptTerms(terms, isWhitelisted: true);

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

        organization.AcceptTerms(termsV1, isWhitelisted: true);
        organization.AcceptTerms(termsV2, isWhitelisted: true);

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

        organization.AcceptTerms(terms, isWhitelisted: true);
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

    [Fact]
    public void CreateTrial_CreatesOrganizationWithTrialStatus()
    {
        var tin = Tin.Create("12345678");
        var organizationName = OrganizationName.Create("Test Organization");

        var organization = Organization.CreateTrial(tin, organizationName);

        organization.Status.Should().Be(OrganizationStatus.Trial);
    }

    [Fact]
    public void Create_CreatesOrganizationWithNormalStatus()
    {
        var tin = Tin.Create("12345678");
        var organizationName = OrganizationName.Create("Test Organization");

        var organization = Organization.Create(tin, organizationName);

        organization.Status.Should().Be(OrganizationStatus.Normal);
    }

    [Fact]
    public void PromoteToNormal_FromTrial_Succeeds()
    {
        var org = Organization.CreateTrial(Tin.Create("12345678"), OrganizationName.Create("Test"));

        org.PromoteToNormal();

        org.Status.Should().Be(OrganizationStatus.Normal);
    }

    [Fact]
    public void PromoteToNormal_FromNonTrial_Throws()
    {
        var org = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test"));

        Action act = () => org.PromoteToNormal();
        act.Should().Throw<BusinessException>()
            .WithMessage("Only trial organizations can be promoted to normal.");
    }

    [Fact]
    public void Deactivate_FromNormal_Succeeds()
    {
        var org = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test"));

        org.Deactivate();

        org.Status.Should().Be(OrganizationStatus.Deactivated);
        org.TermsAccepted.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_FromNonNormal_Throws()
    {
        var org = Organization.CreateTrial(Tin.Create("12345678"), OrganizationName.Create("Test"));

        Action act = () => org.Deactivate();
        act.Should().Throw<BusinessException>()
            .WithMessage("Only normal organizations can be deactivated.");
    }

    [Fact]
    public void Reactivate_FromDeactivated_Succeeds()
    {
        var org = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test"));
        org.Deactivate();

        org.Reactivate();

        org.Status.Should().Be(OrganizationStatus.Normal);
    }

    [Fact]
    public void Reactivate_FromNonDeactivated_Throws()
    {
        var org = Organization.CreateTrial(Tin.Create("12345678"), OrganizationName.Create("Test"));

        Action act = () => org.Reactivate();
        act.Should().Throw<BusinessException>()
            .WithMessage("Only deactivated organizations can be reactivated.");
    }

    [Fact]
    public void AcceptTerms_WithNormalStatus_AndWhitelisted_Succeeds()
    {
        var org = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test"));
        var terms = Terms.Create(1);

        org.AcceptTerms(terms, isWhitelisted: true);

        org.TermsAccepted.Should().BeTrue();
        org.TermsVersion.Should().Be(1);
    }

    [Fact]
    public void AcceptTerms_WithNormalStatus_AndNotWhitelisted_Throws()
    {
        var org = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test"));
        var terms = Terms.Create(1);

        Action act = () => org.AcceptTerms(terms, isWhitelisted: false);

        act.Should().Throw<BusinessException>()
            .WithMessage("Normal organization is no longer whitelisted. Please contact support.");
    }

    [Fact]
    public void AcceptTerms_WithTrialStatus_AndWhitelisted_PromotesToNormal()
    {
        var org = Organization.CreateTrial(Tin.Create("12345678"), OrganizationName.Create("Test"));
        var terms = Terms.Create(1);

        org.AcceptTerms(terms, isWhitelisted: true);

        org.Status.Should().Be(OrganizationStatus.Normal);
        org.TermsAccepted.Should().BeTrue();
        org.TermsVersion.Should().Be(1);
    }

    [Fact]
    public void AcceptTerms_WithTrialStatus_AndNotWhitelisted_RemainsTrialWithTerms()
    {
        var org = Organization.CreateTrial(Tin.Create("12345678"), OrganizationName.Create("Test"));
        var terms = Terms.Create(1);

        org.AcceptTerms(terms, isWhitelisted: false);

        org.Status.Should().Be(OrganizationStatus.Trial);
        org.TermsAccepted.Should().BeTrue();
        org.TermsVersion.Should().Be(1);
    }

    [Fact]
    public void AcceptTerms_WithDeactivatedStatus_AndWhitelisted_ReactivatesWithTerms()
    {
        var org = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test"));
        org.Deactivate();
        var terms = Terms.Create(1);

        org.AcceptTerms(terms, isWhitelisted: true);

        org.Status.Should().Be(OrganizationStatus.Normal);
        org.TermsAccepted.Should().BeTrue();
        org.TermsVersion.Should().Be(1);
    }

    [Fact]
    public void AcceptTerms_WithDeactivatedStatus_AndNotWhitelisted_Throws()
    {
        var org = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test"));
        org.Deactivate();
        var terms = Terms.Create(1);

        Action act = () => org.AcceptTerms(terms, isWhitelisted: false);

        act.Should().Throw<BusinessException>()
            .WithMessage("Deactivated organization is not whitelisted and cannot be reactivated.");
    }
}
