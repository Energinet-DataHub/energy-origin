using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace API.Metrics;

public class AuthorizationCertMetrics : IAuthorizationCertMetrics
{
    public const string MetricName = "AuthorizationCertMetrics";

    private Counter<long> NumberOfUniqueUserLogins { get; }
    private Counter<long> NumberOfUniqueClientOrganizationLogins { get; }


    public AuthorizationCertMetrics()
    {
        var meter = new Meter(MetricName);

        NumberOfUniqueUserLogins = meter.CreateCounter<long>("ett_authorization_unique_user_logins");
        NumberOfUniqueClientOrganizationLogins =
            meter.CreateCounter<long>("ett_authorization_unique_client_organization_logins");
    }

    public void AddUniqueUserLogin(string organizationId, string idpClientId)
    {
        NumberOfUniqueUserLogins.Add(1,
            new KeyValuePair<string, object?>("OrganizationId", HashLabel(organizationId)),
            new KeyValuePair<string, object?>("UserId", HashLabel(idpClientId))
        );
    }

    public void AddUniqueClientOrganizationLogin(string organizationId)
    {
        NumberOfUniqueClientOrganizationLogins.Add(1,
            new KeyValuePair<string, object?>("OrganizationId", HashLabel(organizationId)));
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
