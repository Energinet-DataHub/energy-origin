using Microsoft.Extensions.Logging;

namespace API.Utilities;

public static class LoggerExtensions
{
    public const string AUDIT_SCOPE = "AUDIT";
    public const string AUDIT_MARKER = "AUDIT :: ";
    public static void AuditLog(this ILogger logger, string message, params object?[] args)
    {
        using (logger.BeginScope(AUDIT_SCOPE))
        {
#pragma warning disable CA2254 // NOTE: message _is_ a template!
            logger.LogInformation($"{AUDIT_MARKER}{message}", args);
#pragma warning restore CA2254
        }
    }
}
