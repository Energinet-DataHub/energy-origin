using System;

namespace DataContext.Models;

public enum ReportStatus
{
    Pending,
    Completed,
    Failed
}

public class Report
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ReportStatus Status { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public byte[]? Content { get; set; }
}
