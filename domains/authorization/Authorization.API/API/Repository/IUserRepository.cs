using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Models;

namespace API.Repository;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<IEnumerable<User>> GetAllAsync();
    Task AddAsync(User user);
    Task RemoveAsync(User user);
    Task UpdateAsync(User user);
}
