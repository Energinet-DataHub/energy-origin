using System.Threading.Tasks;

namespace API.Services;

public interface IUserSignUpService
{
    Task ProcessUserSignUpAsync(string token);
}
