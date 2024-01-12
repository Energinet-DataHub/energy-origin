using System;
using System.Text.RegularExpressions;

namespace API.Shared.Exceptions;

[Serializable]
public abstract class SanitizedException : Exception
{
    private static string? clean(string? message)
    {
        if (message == null) return null;

        message = Regex.Replace(message, "([0-9]{10})", "**********");
        return Regex.Replace(message, "([0-9]{6}-[0-9]{4})", "******-****");
    }

    protected SanitizedException(string? message) : base(clean(message))
    {
    }

    protected SanitizedException(string? message, Exception? innerException) : base(clean(message), innerException)
    {
    }
}
