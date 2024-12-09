using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using API.ValueObjects;

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
        NumberOfUniqueClientOrganizationLogins =
            meter.CreateCounter<long>("ett_authorization_unique_client_organization_logins");
    }

    public void AddUniqueUserLogin(string organizationId, IdpUserId idpClientId)
    {
        NumberOfUniqueUserLogins.Add(1,
            new KeyValuePair<string, object?>("OrganizationId", HashLabel(organizationId)),
            new KeyValuePair<string, object?>("UserId", HashLabel(idpClientId.Value.ToString()))
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
