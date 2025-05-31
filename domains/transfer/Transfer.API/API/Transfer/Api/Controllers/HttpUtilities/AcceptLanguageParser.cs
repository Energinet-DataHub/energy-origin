using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace API.Transfer.Api.Controllers.HttpUtilities;

public static class AcceptLanguageParser
{
    public static string GetPreferredLanguage(IHeaderDictionary headers)
    {
        var header = headers["Accept-Language"].ToString();
        if (string.IsNullOrWhiteSpace(header))
            return "en";

        var languages = header
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(item =>
            {
                var parts = item.Split(';', StringSplitOptions.RemoveEmptyEntries);
                var lang = parts[0].Trim();
                var q = 1.0;

                if (parts.Length > 1 && parts[1].StartsWith("q=", StringComparison.OrdinalIgnoreCase))
                {
                    var qStr = parts[1].Substring(2);
                    if (!double.TryParse(qStr, NumberStyles.Any, CultureInfo.InvariantCulture, out q))
                        q = 0.0;
                }

                return (lang, q);
            })
            .Where(t => !string.IsNullOrEmpty(t.lang))
            .OrderByDescending(t => t.q)
            .Select(t => t.lang)
            .ToList();

        return languages.FirstOrDefault() ?? "en";
    }
}
