
namespace API.ContractService.Metrics;

public interface IAuthorizationMetricsCert
{
    void AddUniqueUserLogin(string organizationId, string idpClientId);
    void AddUniqueClientOrganizationLogin(string orginizationId);
}
