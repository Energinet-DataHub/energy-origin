namespace API.Utilities;

public static class LoggerExtensions
{
    private const string auditScope = "AUDIT";
    private const string auditMarker = "AUDIT :: ";
    public static void AuditLog(this ILogger logger, string message, params object?[] args)
    {
        using (logger.BeginScope(auditScope))
        {
#pragma warning disable CA2254 // NOTE: message _is_ a template!
            logger.LogInformation($"{auditMarker}{message}", args);
#pragma warning restore CA2254
        }
    }
}
