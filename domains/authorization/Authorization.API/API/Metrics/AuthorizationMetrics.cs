using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace API.Metrics;

public class AuthorizationMetrics : IAuthorizationMetrics
{
    public const string MetricName = "Authorization";

    private Counter<long> NumberOfUniqueUserLogins { get; }
    private Counter<long> NumberOfUniqueClientOrganizationLogins { get; }


    public AuthorizationMetrics()
    {
        var meter = new Meter(MetricName);

        NumberOfUniqueUserLogins = meter.CreateCounter<long>("ett_authorization_unique_user_logins");
        NumberOfUniqueClientOrganizationLogins = meter.CreateCounter<long>("ett_authorization_unique_client_organization_logins");
    }

    public void AddUniqueUserLogin(IList<KeyValuePair<string, object?>> labels)
    {
        var hashedLabels = labels
            .Select(label => new KeyValuePair<string, object?>(label.Key, HashLabel($"{label.Value}")));

        NumberOfUniqueUserLogins.Add(1, hashedLabels.ToArray());
    }

    public void AddUniqueClientOrganizationLogin(string labelKey, string? labelValue)
    {
        NumberOfUniqueClientOrganizationLogins.Add(1, new KeyValuePair<string, object?>(labelKey, HashLabel($"{labelValue}")));
    }

    private string HashLabel(string label)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(label);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
