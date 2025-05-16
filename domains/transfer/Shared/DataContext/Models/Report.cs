using System;
using EnergyOrigin.Domain.ValueObjects;

namespace DataContext.Models;

public enum ReportStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2
}

public class Report
{
    public Guid Id { get; set; }
    public ReportStatus Status { get; set; }
    public UnixTimestamp CreatedAt { get; set; } = UnixTimestamp.Now();
    public UnixTimestamp StartDate { get; set; } = UnixTimestamp.Empty();
    public UnixTimestamp EndDate { get; set; } = UnixTimestamp.Empty();
    public byte[]? Content { get; set; }
}
