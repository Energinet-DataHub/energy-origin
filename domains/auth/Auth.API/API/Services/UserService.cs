using API.Models.Entities;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using API.Utilities;
using EnergyOrigin.IntegrationEvents.Events.Terms.V2;
using MassTransit;

namespace API.Services;

public class UserService : IUserService
{
    private readonly IUserRepository repository;
    private readonly IPublishEndpoint publishEndpoint;

    public UserService(IUserRepository repository, IPublishEndpoint publishEndpoint)
    {
        this.repository = repository;
        this.publishEndpoint = publishEndpoint;
    }

    public async Task<User> UpsertUserAsync(User user) => await repository.UpsertUserAsync(user);

    public async Task UpdateTermsAccepted(User user, DecodableUserDescriptor descriptor, string traceId)
    {
        repository.UpdateTermsAccepted(user);
        await publishEndpoint.Publish(new OrgAcceptedTerms(Guid.NewGuid(), traceId, DateTimeOffset.UtcNow, descriptor.Subject, descriptor.Organization?.Tin, descriptor.Id));
        await repository.SaveChangeAsync();
    }
    public async Task<User?> GetUserByIdAsync(Guid? id) => id is null ? null : await repository.GetUserByIdAsync(id.Value);
    public async Task<User> InsertUserAsync(User user) => await repository.InsertUserAsync(user);
    public async Task RemoveUserAsync(User user) => await repository.RemoveUserAsync(user);
    public async Task<IEnumerable<User>> GetUsersByTinAsync(string tin) => await repository.GetUsersByTinAsync(tin);
}
