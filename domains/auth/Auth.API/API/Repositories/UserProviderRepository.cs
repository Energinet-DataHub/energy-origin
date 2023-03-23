using API.Models.Entities;
using API.Repositories.Data.Interfaces;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql.NameTranslation;

namespace API.Repositories;

public class UserProviderRepository : IUserProviderRepository
{
    private readonly IUserProviderDataContext dataContext;

    public UserProviderRepository(IUserProviderDataContext dataContext) => this.dataContext = dataContext;

    public async Task<UserProvider> UpsertUserProviderAsync(UserProvider userProvider)
    {
        dataContext.UserProviders.Update(userProvider);
        await dataContext.SaveChangesAsync();
        return userProvider;
    }
    public async Task<UserProvider?> GetUserProviderByIdAsync(Guid id) => await dataContext.UserProviders.FirstOrDefaultAsync(x => x.Id == id);
    public async Task<UserProvider?> FindUserProviderMatchAsync(List<UserProvider> userProviders) =>
        await dataContext.UserProviders.Include(x => x.User)
            .Join(ConvertInMemoryListToDatabaseQueryable(userProviders),
                db => new { keyType = db.ProviderKeyType, key = db.UserProviderKey, type = db.ProviderType },
                im => new { keyType = im.ProviderKeyType, key = im.UserProviderKey, type = im.ProviderType },
                (db, im) => db)
            .SingleOrDefaultAsync();

    private IQueryable<UserProvider> ConvertInMemoryListToDatabaseQueryable(List<UserProvider> userProviders)
    {
        var sql = $"SELECT * FROM (VALUES "
        + $"{string.Join(", ",
            userProviders.Select(x => $"("
                + $"'{Guid.Empty}'::uuid, "
                + $"'{NpgsqlSnakeCaseNameTranslator.ConvertToSnakeCase(x.ProviderType.ToString())}'::{NpgsqlSnakeCaseNameTranslator.ConvertToSnakeCase(nameof(UserProvider.ProviderType))}, "
                + $"'{NpgsqlSnakeCaseNameTranslator.ConvertToSnakeCase(x.ProviderKeyType.ToString())}'::{NpgsqlSnakeCaseNameTranslator.ConvertToSnakeCase(nameof(UserProvider.ProviderKeyType))}, "
                + $"'{x.UserProviderKey}'::text, "
                + $"'{Guid.Empty}'::uuid"
            + $")"
        ))}"
        + $")"
        + $"""AS t("{nameof(UserProvider.Id)}", "{nameof(UserProvider.ProviderType)}", "{nameof(UserProvider.ProviderKeyType)}", "{nameof(UserProvider.UserProviderKey)}", "{nameof(UserProvider.UserId)}")""";

        return dataContext.UserProviders.FromSqlRaw(sql);
    }
}
