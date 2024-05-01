using API.Authorization._Features_;
using API.Models;
using API.UnitTests.Repository;
using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests._Queries_;

public class GetConsentForClientQueryTests
{


    [Fact]
    public async Task Return_External_Client_When_Everything_Is_Awesome()
    {
        // Arrange
        var clientRepository = new FakeClientRepository();
        var clientId = new IdpClientId(Guid.NewGuid());
        await clientRepository.AddAsync(Client.Create(clientId, new("External"), Role.External));
        var sut = new GetConsentForClientQueryHandler(clientRepository);

        // Act
        var result = await sut.Handle(new(clientId.Value.ToString()), CancellationToken.None);

        // Assert
        result.Name.Should().Be("External");
        result.OrgName.Should().Be("External");

    }
}


// public class FakeClientRepository : IClientRepository
// {
//     public List<Client> Clients { get; set; } = new();
//
//     public FakeClientRepository()
//     {
//         Clients = new List<Client>().ToListAsync();
//     }
//
//     public IQueryable<Client> Where(Expression<Func<Client, bool>> predicate)
//     {
//         return Query().Where(predicate);
//     }
//
//     public IQueryable<Client> Query()
//     {
//         return Clients.AsQueryable();
//     }
//
//     public Task<Client> GetAsync(Guid id)
//     {
//         return Task.FromResult(Clients.First(x => x.Id == id));
//     }
//
//     public Task AddAsync(Client entity)
//     {
//         Clients.Add(entity);
//         return Task.CompletedTask;
//     }
//
//     public Task RemoveAsync(Client entity)
//     {
//         Clients.Remove(entity);
//         return Task.CompletedTask;
//     }
//
//     public async Task UpdateAsync(Client entity)
//     {
//         Clients.Remove(await GetAsync(entity.Id));
//         Clients.Add(entity);
//     }
// }
