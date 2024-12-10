using System.Collections.Generic;

namespace API.Metrics;

public interface IAuthorizationCertMetrics
{
    void AddUniqueUserLogin(string organizationId, string idpClientId);
    void AddUniqueClientOrganizationLogin(string orginizationId);
}
