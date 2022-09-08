using System.Data.SQLite;
using Dapper;
using EnergyOriginEventStore.EventStore.Database;

namespace EnergyOriginEventStore.Tests;

public class SqliteDatabaseEventContext : DatabaseEventContext
{
    private readonly SQLiteConnection connection = new("DataSource=:memory:");

    public async Task Setup()
    {
        await connection.OpenAsync();
        await connection.ExecuteAsync("CREATE TABLE messages(id INTEGER PRIMARY KEY, topic TEXT NOT NULL, payload TEXT NOT NULL);");
        await connection.ExecuteAsync("CREATE INDEX index_messages_topic on messages(topic);");
    }

    public async Task Clean() => await connection.ExecuteAsync("DELETE FROM messages;");

    async Task DatabaseEventContext.Add(Message message) => await connection.ExecuteAsync("INSERT INTO messages VALUES(NULL, @topic, @payload);", new { topic = message.Topic, payload = message.Payload });

    async Task<Message?> DatabaseEventContext.NextAfter(long id, string topicPrefix) => await connection.QueryFirstOrDefaultAsync<Message?>("SELECT id, topic, payload FROM messages WHERE id > @id AND topic LIKE @prefix ORDER BY id ASC LIMIT 1;", new { id, prefix = $"{topicPrefix}%" });
}
