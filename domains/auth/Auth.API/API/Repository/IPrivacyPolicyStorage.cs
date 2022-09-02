using API.Models;

namespace API.Repository;

public interface IPrivacyPolicyStorage
{
    Task<PrivacyPolicy> Get();
}
