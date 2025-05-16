namespace EnergyOrigin.IntegrationEvents.Events.Pdf.V1;

public record ReportRequestCreated : IntegrationEvent
{
    public Guid ReportId { get; init; }
    public long StartDate { get; init; }
    public long EndDate { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; }

    public ReportRequestCreated(
        Guid reportId,
        long startDate,
        long endDate,
        IDictionary<string, object>? metadata = null
    )
    {
        ReportId = reportId;
        StartDate = startDate;
        EndDate = endDate;
        Metadata = metadata is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(metadata);
    }
}
