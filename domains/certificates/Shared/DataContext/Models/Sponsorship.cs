using System;
using EnergyOrigin.Domain.ValueObjects;

namespace DataContext.Models;

public class Sponsorship
{
    public required Gsrn SponsorshipGSRN { get; init; }
    public DateTimeOffset SponsorshipEndDate { get; init; }
}
