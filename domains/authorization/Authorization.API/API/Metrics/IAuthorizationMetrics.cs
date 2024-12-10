using System.Collections.Generic;
using API.ValueObjects;

namespace API.Metrics;

public interface IAuthorizationMetrics
{
    void AddUniqueUserLogin(string organizationId, IdpUserId idpClientId);
    void AddUniqueClientOrganizationLogin(string orginizationId);
}
