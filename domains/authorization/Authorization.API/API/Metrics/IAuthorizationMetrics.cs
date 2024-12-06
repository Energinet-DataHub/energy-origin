using System.Collections.Generic;

namespace API.Metrics;

public interface IAuthorizationMetrics
{
    void AddUniqueUserLogin(IList<KeyValuePair<string, object?>> labels);
    void AddUniqueClientOrganizationLogin(string labelKey, string? labelValue);
}
