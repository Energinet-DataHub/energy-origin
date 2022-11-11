namespace CertificateEvents;

public static class Topic
{
    public const string MeasurementPrefix = "measurements";
    public static string For(MeasurementBaseEvent @event) => $"{MeasurementPrefix}/{@event.GSRN}";
}
