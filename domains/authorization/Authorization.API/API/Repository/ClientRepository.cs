using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using API.Models;

namespace API.Repository;

public class ClientRepository(ApplicationDbContext context) : IClientRepository
{
    public async Task<Client?> GetByIdAsync(Guid id)
    {
        return await context.Clients.FindAsync(id);
    }

    public async Task<IEnumerable<Client>> GetAllAsync()
    {
        return await context.Clients.ToListAsync();
    }

    public async Task AddAsync(Client client)
    {
        ArgumentNullException.ThrowIfNull(client);

        await context.Clients.AddAsync(client);
    }

    public async Task RemoveAsync(Client client)
    {
        ArgumentNullException.ThrowIfNull(client);

        context.Clients.Remove(client);
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(Client client)
    {
        ArgumentNullException.ThrowIfNull(client);

        context.Clients.Update(client);
        await Task.CompletedTask;
    }
}
