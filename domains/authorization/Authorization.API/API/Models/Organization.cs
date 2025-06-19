using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup.Exceptions;

namespace API.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrganizationStatus
{
    [EnumMember(Value = "Trial")]
    Trial,

    [EnumMember(Value = "Normal")]
    Normal,

    [EnumMember(Value = "Deactivated")]
    Deactivated
}

public class Organization : IEntity<Guid>
{
    private Organization(
        Guid id,
        Tin? tin,
        OrganizationName organizationName,
        OrganizationStatus status
    )
    {
        Id = id;
        Tin = tin;
        Name = organizationName;
        Status = status;
        TermsAccepted = false;
        TermsVersion = null;
        TermsAcceptanceDate = null;
        ServiceProviderTermsAccepted = false;
        ServiceProviderTermsAcceptanceDate = null;
    }

    private Organization()
    {
    }

    public Guid Id { get; private set; }
    public Tin? Tin { get; private set; } = null!;
    public OrganizationName Name { get; private set; } = null!;
    public OrganizationStatus Status { get; private set; }
    public bool TermsAccepted { get; private set; }
    public int? TermsVersion { get; private set; }
    public DateTimeOffset? TermsAcceptanceDate { get; private set; }
    public bool ServiceProviderTermsAccepted { get; private set; }
    public UnixTimestamp? ServiceProviderTermsAcceptanceDate { get; private set; }
    public ICollection<Affiliation> Affiliations { get; init; } = new List<Affiliation>();
    public ICollection<OrganizationConsent> OrganizationGivenConsents { get; init; } = new List<OrganizationConsent>();
    public ICollection<OrganizationConsent> OrganizationReceivedConsents { get; init; } = new List<OrganizationConsent>();
    public ICollection<Client> Clients { get; init; } = new List<Client>();

    public static Organization Create(
        Tin? tin,
        OrganizationName organizationName
    )
    {
        return new Organization(
            Guid.NewGuid(),
            tin,
            organizationName,
            OrganizationStatus.Normal
        );
    }

    public static Organization CreateTrial(
        Tin tin,
        OrganizationName organizationName
    )
    {
        return new Organization(
            Guid.NewGuid(),
            tin,
            organizationName,
            OrganizationStatus.Trial
        );
    }

    public void AcceptTerms(Terms terms, bool isWhitelisted)
    {
        if (Status is not (OrganizationStatus.Trial or OrganizationStatus.Normal or OrganizationStatus.Deactivated))
        {
            throw new BusinessException(
                $"Unexpected organization state: {Status} (whitelisted: {isWhitelisted})");
        }

        switch (Status, isWhitelisted)
        {
            case (OrganizationStatus.Normal, isWhitelisted: false):
                throw new BusinessException("Normal organization is no longer whitelisted. Please contact support.");

            case (OrganizationStatus.Deactivated, isWhitelisted: false):
                throw new BusinessException("Deactivated organization is not whitelisted and cannot be reactivated.");

            case (OrganizationStatus.Trial, isWhitelisted: true):
                PromoteToNormal();
                break;

            case (OrganizationStatus.Deactivated, isWhitelisted: true):
                Reactivate();
                break;

            case (OrganizationStatus.Trial, isWhitelisted: false):
                break;

            case (OrganizationStatus.Normal, isWhitelisted: true):
                break;

            // Defensive programming
            default:
                throw new BusinessException(
                    $"Unexpected organization state: {Status} (whitelisted: {isWhitelisted})");
        }

        TermsAccepted = true;
        TermsVersion = terms.Version;
        TermsAcceptanceDate = DateTimeOffset.UtcNow;
    }

    public void PromoteToNormal()
    {
        if (Status != OrganizationStatus.Trial)
            throw new BusinessException("Only trial organizations can be promoted to normal.");

        Status = OrganizationStatus.Normal;
        RevokeTerms();
        RevokeServiceProviderTerms();
    }

    public void Deactivate()
    {
        if (Status != OrganizationStatus.Normal)
            throw new BusinessException("Only normal organizations can be deactivated.");

        Status = OrganizationStatus.Deactivated;
        RevokeTerms();
        RevokeServiceProviderTerms();
    }

    public void Reactivate()
    {
        if (Status != OrganizationStatus.Deactivated)
            throw new BusinessException("Only deactivated organizations can be reactivated.");

        Status = OrganizationStatus.Normal;
    }

    public void AcceptServiceProviderTerms()
    {
        ServiceProviderTermsAccepted = true;
        ServiceProviderTermsAcceptanceDate = UnixTimestamp.Now();
    }

    private void RevokeServiceProviderTerms()
    {
        ServiceProviderTermsAccepted = false;
        ServiceProviderTermsAcceptanceDate = null;
    }

    public void InvalidateTerms() => TermsAccepted = false;

    public void RevokeTerms()
    {
        TermsAccepted = false;
        TermsVersion = null;
        TermsAcceptanceDate = null;
    }
}
