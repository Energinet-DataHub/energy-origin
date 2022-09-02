using Dapper;
using EnergyOriginEventStore.EventStore.Database;
using Npgsql;

namespace EnergyOriginEventStore.EventStore.Database;

public class PostgresDatabaseEventContext : DatabaseEventContext
{
    private readonly NpgsqlConnection connection;

    public PostgresDatabaseEventContext(NpgsqlConnection connection) => this.connection = connection;

    async Task DatabaseEventContext.Add(Message message)
    {
        var transaction = await connection.BeginTransactionAsync();
        try
        {
            await connection.ExecuteAsync("INSERT INTO messages VALUES(NULL, @topic, @payload);", new { topic = message.Topic, payload = message.Payload });
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }

    async Task<Message?> DatabaseEventContext.NextAfter(long id, string topicPrefix) => await connection.QueryFirstOrDefaultAsync<Message?>("SELECT id, topic, payload FROM messages WHERE id > @id AND topic LIKE @prefix ORDER BY id ASC LIMIT 1;", new { id, prefix = $"{topicPrefix}%" });
}
