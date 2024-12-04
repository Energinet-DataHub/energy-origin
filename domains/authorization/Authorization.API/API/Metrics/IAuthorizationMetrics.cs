namespace API.Metrics;

public interface IAuthorizationMetrics
{
    void AddUniqueUserLogin(string labelKey, string? labelValue);
    void AddUniqueUserOrganizationLogin(string labelKey, string? labelValue);
    void AddUniqueClientLogin(string labelKey, string? labelValue);
    void AddUniqueClientOrganizationLogin(string labelKey, string? labelValue);
    void AddTotalLogin();
}
