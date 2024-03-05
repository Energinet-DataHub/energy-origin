using API.Models.Entities;
using API.Repositories.Data;
using API.Repositories.Data.Interfaces;
using API.Repositories.Interfaces;
using EnergyOrigin.IntegrationEvents.Events;
using MassTransit;
using MassTransit.RabbitMqTransport;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DataContext dataContext;
    private readonly IPublishEndpoint publishEndpoint;

    public UserRepository(DataContext dataContext, IPublishEndpoint publishEndpoint)
    {
        this.dataContext = dataContext;
        this.publishEndpoint = publishEndpoint;
    }

    public async Task<User> UpsertUserAsync(User user)
    {
        dataContext.Users.Update(user);
        await dataContext.SaveChangesAsync();
        return user;
    }
    public async Task<User?> GetUserByIdAsync(Guid id) => await dataContext.Users.Include(x => x.Company).ThenInclude(x => x!.CompanyTerms).Include(x => x.UserProviders).Include(x => x.UserTerms).Include(x => x.UserRoles).SingleOrDefaultAsync(x => x.Id == id);
    public async Task<User> InsertUserAsync(User user)
    {
        await dataContext.Users.AddAsync(user);
        await dataContext.SaveChangesAsync();
        return user;
    }
    public async Task RemoveUserAsync(User user)
    {
        dataContext.Users.Remove(user);
        await dataContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<User>> GetUsersByTinAsync(string tin) => await dataContext.Users.Where(x => x.Company != null && x.Company.Tin == tin).Include(u => u.UserRoles).ToListAsync();
    public async Task<User> UpdateTermsAccepted(User user)
    {
        dataContext.Users.Update(user);
        await publishEndpoint.Publish(new OrgAcceptedTerms((Guid)user.Id!, "12345678"));

        await dataContext.SaveChangesAsync();
        return user;
    }
}
