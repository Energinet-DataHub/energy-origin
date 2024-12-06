using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Security.Cryptography;
using System.Text;

namespace API.Metrics;

public class AuthorizationMetrics : IAuthorizationMetrics
{
    public const string MetricName = "Authorization";

    private Counter<long> NumberOfUniqueUserLogins { get; }
    private Counter<long> NumberOfUniqueUserOrganizationLogins { get; }
    private Counter<long> NumberOfUniqueClientLogins { get; }
    private Counter<long> NumberOfUniqueClientOrganizationLogins { get; }

    private ObservableCounter<long> TotalNumberOfLogins { get; }
    private long _totalNumberOfLogins;

    public AuthorizationMetrics()
    {
        var meter = new Meter(MetricName);

        NumberOfUniqueUserLogins = meter.CreateCounter<long>("ett_authorization_unique_user_logins");
        NumberOfUniqueUserOrganizationLogins = meter.CreateCounter<long>("ett_authorization_unique_user_organization_logins");
        NumberOfUniqueClientLogins = meter.CreateCounter<long>("ett_authorization_unique_client_logins");
        NumberOfUniqueClientOrganizationLogins = meter.CreateCounter<long>("ett_authorization_unique_client_organization_logins");
        TotalNumberOfLogins = meter.CreateObservableCounter("ett_authorization_total_logins", () => _totalNumberOfLogins);
    }

    public void AddUniqueUserLogin(string labelKey, string? labelValue)
    {
        NumberOfUniqueUserLogins.Add(1, new KeyValuePair<string, object?>(labelKey, HashLabel($"{labelValue}")));
    }

    public void AddUniqueUserOrganizationLogin(string labelKey, string? labelValue)
    {
        NumberOfUniqueUserOrganizationLogins.Add(1, new KeyValuePair<string, object?>(labelKey, HashLabel($"{labelValue}")));
    }

    public void AddUniqueClientLogin(string labelKey, string? labelValue)
    {
        NumberOfUniqueClientLogins.Add(1, new KeyValuePair<string, object?>(labelKey, HashLabel($"{labelValue}")));
    }

    public void AddUniqueClientOrganizationLogin(string labelKey, string? labelValue)
    {
        NumberOfUniqueClientOrganizationLogins.Add(1, new KeyValuePair<string, object?>(labelKey, HashLabel($"{labelValue}")));
    }

    public void AddTotalLogin()
    {
        _totalNumberOfLogins++;
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
