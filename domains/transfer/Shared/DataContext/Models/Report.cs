using System;
using System.Text.Json.Serialization;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup.Exceptions;

namespace DataContext.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2
}

public class Report
{
    private Report(
        Guid id,
        OrganizationId organizationId,
        OrganizationName organizationName,
        Tin organizationTin,
        UnixTimestamp createdAt,
        UnixTimestamp startDate,
        UnixTimestamp endDate,
        ReportStatus status,
        bool isTrial,
        byte[]? content)
    {
        Id = id;
        OrganizationId = organizationId ?? throw new ArgumentNullException(nameof(organizationId));
        OrganizationName = organizationName;
        OrganizationTin = organizationTin;
        CreatedAt = createdAt;
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
        IsTrial = isTrial;
        Content = content;
    }

    private Report() { }

    public static Report Create(
        Guid id,
        OrganizationId organizationId,
        OrganizationName organizationName,
        Tin organizationTin,
        string orgStatus,
        UnixTimestamp startDate,
        UnixTimestamp endDate)
    {
        if (organizationId.Value == Guid.Empty)
            throw new BusinessException(nameof(organizationId));

        var createdAt = UnixTimestamp.Now();

        if (endDate > createdAt)
            throw new BusinessException("EndDate cannot be in the future.");

        if (endDate < startDate.AddDays(7) || endDate > startDate.AddYears(1))
            throw new BusinessException("Date range must be between 1 week and 1 year.");

        if (orgStatus is not ("trial" or "normal"))
            throw new BusinessException("Organization status must be either 'trial' or 'normal'");

        return new Report(
            id: id,
            organizationId: organizationId,
            organizationTin: organizationTin,
            organizationName: organizationName,
            createdAt: createdAt,
            startDate: startDate,
            endDate: endDate,
            status: ReportStatus.Pending,
            isTrial: orgStatus == "trial",
            content: null
        );
    }

    public Guid Id { get; private set; }
    public OrganizationId OrganizationId { get; private set; } = OrganizationId.Empty();
    public OrganizationName OrganizationName { get; private set; } = OrganizationName.Empty();
    public Tin OrganizationTin { get; private set; } = Tin.Empty();
    public UnixTimestamp CreatedAt { get; private set; } = UnixTimestamp.Empty();
    public UnixTimestamp StartDate { get; private set; } = UnixTimestamp.Empty();
    public UnixTimestamp EndDate { get; private set; } = UnixTimestamp.Empty();
    public ReportStatus Status { get; private set; }
    public bool IsTrial { get; private set; }
    public byte[]? Content { get; private set; }

    public void MarkCompleted(byte[] pdfBytes)
    {
        if (Status != ReportStatus.Pending)
            throw new InvalidOperationException(
                "Only Pending reports can be completed.");
        if (pdfBytes is null || pdfBytes.Length == 0)
            throw new ArgumentException(
                "PDF content must be provided.", nameof(pdfBytes));

        Content = pdfBytes;
        Status = ReportStatus.Completed;
    }

    public void MarkFailed()
    {
        if (Status != ReportStatus.Pending)
            throw new InvalidOperationException(
                "Only Pending reports can be failed.");

        Status = ReportStatus.Failed;
    }
}
