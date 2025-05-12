using System;
using EnergyOrigin.Domain.ValueObjects;

namespace DataContext.Models;

public enum SponsorshipState
{
    Active,
    Expired
}

public class Sponsorship
{
    private Sponsorship(Gsrn gsrn, DateTimeOffset sponsorshipEndDate)
    {
        Gsrn = gsrn;
        SponsorshipEndDate = sponsorshipEndDate;
    }

    private Sponsorship() { }

    public Gsrn Gsrn { get; private set; } = null!;
    public DateTimeOffset SponsorshipEndDate { get; private set; }

    public static Sponsorship Create(Gsrn gsrn, DateTimeOffset sponsorshipEndDate)
        => new Sponsorship(gsrn, sponsorshipEndDate);

    public SponsorshipState State(DateTimeOffset referenceDate)
        => SponsorshipEndDate > referenceDate
            ? SponsorshipState.Active
            : SponsorshipState.Expired;
}
