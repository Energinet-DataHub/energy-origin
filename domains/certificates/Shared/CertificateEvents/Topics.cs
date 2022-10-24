namespace CertificateEvents;

public static class Topics
{
    public const string MeasurementPrefix = "measurements";
    public static string Measurement(string id) => $"{MeasurementPrefix}/{id}";

    public const string CertificatePrefix = "certificates";
    public static string Certificate(string id) => $"{CertificatePrefix}/{id}";
}
